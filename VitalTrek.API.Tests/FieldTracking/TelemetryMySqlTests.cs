using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TelemetryMySqlTests
{
    [LocalMySqlFact]
    public async Task ExistingCapturesKeepTheirTimesAndReceiveDistinctLegacyIdentities()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync(migration: "20261125213341_EnforceSingleActiveTrackingSession");
        await using var context = database.Open();
        var sessionId = await SeedSession(context);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO telemetry_records (session_id, wearable_device_id, latitude, longitude, altitude, recorded_at, synced_at)
            VALUES ({sessionId}, 70, -9.5, -77.5, 3000, '2026-11-01 10:00:00', '2026-11-02 11:00:00'),
                   ({sessionId}, 70, -9.4, -77.4, 3100, '2026-11-01 10:01:00', '2026-11-02 11:01:00');
            """);
        await context.Database.MigrateAsync();
        context.ChangeTracker.Clear();
        var records = await context.Set<TelemetryRecord>().OrderBy(record => record.Id).ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.Equal(2, records.Select(record => record.ClientRecordId).Distinct().Count());
        Assert.All(records, record =>
        {
            Assert.NotEqual(Guid.Empty, record.ClientRecordId);
            Assert.Null(record.MemberId);
            Assert.Null(record.HeartRate);
            Assert.Null(record.Battery);
            Assert.Null(record.DeviationMeters);
        });
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero), records[0].RecordedAt);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 11, 0, 0, TimeSpan.Zero), records[0].SynchronizedAt);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [LocalMySqlFact]
    public async Task FacadeDoesNotCommitAndTheDatabaseRejectsDuplicateIdentityInTheSameSession()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var context = database.Open();
        var sessionId = await SeedSession(context);
        var (service, facade) = Services(context);
        var entry = Entry();
        var input = Input(entry);
        var actor = new TrackingStartData().Actor;
        Assert.Equal(TelemetryReadingStageStatus.Accepted,
            (await facade.StageTelemetryReadingAsync(actor, sessionId, input)).Status);
        await using (var persisted = database.Open()) Assert.Empty(await persisted.Set<TelemetryRecord>().ToListAsync());
        Assert.Equal(TelemetryReadingStageStatus.Duplicate,
            (await facade.StageTelemetryReadingAsync(actor, sessionId, input)).Status);
        await new UnitOfWork(context).CompleteAsync();
        context.ChangeTracker.Clear();
        var stored = await context.Set<TelemetryRecord>().SingleAsync();
        Assert.Equal(60, stored.MemberId);
        Assert.Equal(entry.HeartRate, stored.HeartRate);
        Assert.Equal(entry.Battery, stored.Battery);
        Assert.Equal(entry.DeviationMeters, stored.DeviationMeters);
        Assert.Equal(entry.RecordedAt.Ticks - entry.RecordedAt.Ticks % 10, stored.RecordedAt.Ticks);
        context.Add(new TelemetryRecord(sessionId, 70, entry, 60));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.Single(await context.Set<TelemetryRecord>().ToListAsync());
    }

    [LocalMySqlFact]
    public async Task ConcurrentIdenticalRequestsCommitOneCaptureAndReturnTheSamePersistedRecord()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var setup = database.Open();
        var sessionId = await SeedSession(setup);
        await using var firstContext = database.Open();
        await using var secondContext = database.Open();
        var arrivals = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task BeforeSave()
        {
            if (Interlocked.Increment(ref arrivals) == 2) gate.TrySetResult();
            await gate.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        var first = Services(firstContext, new GatedUnitOfWork(new UnitOfWork(firstContext), BeforeSave)).Service;
        var second = Services(secondContext, new GatedUnitOfWork(new UnitOfWork(secondContext), BeforeSave)).Service;
        var command = new SubmitTelemetryBatchCommand(1, sessionId, 70, [Entry()]);
        var actor = new TrackingStartData().Actor;
        var results = await Task.WhenAll(first.Handle(command, actor, default), second.Handle(command, actor, default));
        var records = results.Select(result => Assert.Single(Assert.IsType<Result<IReadOnlyList<TelemetryRecord>,
            ESubmitTelemetryBatchError>.Success>(result).Value)).ToArray();
        Assert.Equal(records[0].Id, records[1].Id);
        Assert.Equal(records[0].RecordedAt, records[1].RecordedAt);
        Assert.Equal(records[0].SynchronizedAt, records[1].SynchronizedAt);
        await using var persisted = database.Open();
        Assert.Single(await persisted.Set<TelemetryRecord>().ToListAsync());
    }

    [LocalMySqlFact]
    public async Task CommitFailureLeavesNoPartiallySavedReadings()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var setup = database.Open();
        var sessionId = await SeedSession(setup);
        var fault = new FailingCommit();
        await using var context = database.Open(fault);
        var result = await Services(context).Service.Handle(new SubmitTelemetryBatchCommand(1, sessionId, 70,
            [Entry(), Entry()]), new TrackingStartData().Actor, default);
        Assert.True(fault.Reached);
        Assert.Equal(ESubmitTelemetryBatchError.UnexpectedError,
            Assert.IsType<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure>(result).Error);
        Assert.Empty(context.ChangeTracker.Entries());
        await using var persisted = database.Open();
        Assert.Empty(await persisted.Set<TelemetryRecord>().ToListAsync());
    }

    [LocalMySqlFact]
    public async Task StableIdentityIsScopedToItsSession()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var context = database.Open();
        var firstId = await SeedSession(context);
        var first = await context.Set<TrackingSession>().SingleAsync();
        first.End();
        await new UnitOfWork(context).CompleteAsync();
        var second = new TrackingSession(new StartTrackingSessionCommand(1, 101));
        context.Add(second);
        await new UnitOfWork(context).CompleteAsync();
        var entry = Entry();
        context.Add(new TelemetryRecord(firstId, 70, entry, 60));
        context.Add(new TelemetryRecord(second.Id, 70, entry, 60));
        await new UnitOfWork(context).CompleteAsync();
        Assert.Equal(2, await context.Set<TelemetryRecord>().CountAsync());
    }

    private static TelemetryRecordEntry Entry() => new(-9.543212345, -77.543212345, 3020.125,
        DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid(), 92, 75, 45.25);

    private static TelemetryReadingInput Input(TelemetryRecordEntry entry) => new(entry.ClientRecordId, 70,
        entry.RecordedAt, entry.Latitude, entry.Longitude, entry.Altitude, entry.HeartRate, entry.Battery, entry.DeviationMeters);

    private static async Task<int> SeedSession(AppDbContext context)
    {
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        (await context.Set<ExpeditionGroup>().SingleAsync()).Start(startedAt);
        var session = new TrackingSession(new StartTrackingSessionCommand(1, 101), startedAt);
        context.Add(session);
        await new UnitOfWork(context).CompleteAsync();
        return session.Id;
    }

    private static (TelemetryRecordCommandService Service, FieldTrackingContextFacade Facade) Services(
        AppDbContext context, IUnitOfWork? unitOfWork = null)
    {
        var sessions = new TrackingSessionRepository(context);
        var expedition = new ExpeditionSetupContextFacade(new FieldGuideRepository(context),
            new ManifestEntryRepository(context), new ExpeditionGroupRepository(context), new CheckpointRepository(context),
            new ExpectedTimeWindowRepository(context), InterfaceStub<IExpeditionStartService>.Create(),
            new ParticipantPreparationRepository(context), new WearableDeviceRepository(context));
        var service = new TelemetryRecordCommandService(sessions, new TelemetryRecordRepository(context), expedition,
            unitOfWork ?? new UnitOfWork(context), NullLogger<TelemetryRecordCommandService>.Instance);
        return (service, new FieldTrackingContextFacade(sessions, expedition, service));
    }

    private sealed class GatedUnitOfWork(IUnitOfWork inner, Func<Task> beforeSave) : IUnitOfWork
    {
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await beforeSave();
            await inner.CompleteAsync(cancellationToken);
        }
        public void DiscardChanges() => inner.DiscardChanges();
    }

    private sealed class FailingCommit : DbTransactionInterceptor
    {
        public bool Reached { get; private set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Reached = true;
            throw new InvalidOperationException("Test failure before telemetry transaction commit.");
        }
    }
}
