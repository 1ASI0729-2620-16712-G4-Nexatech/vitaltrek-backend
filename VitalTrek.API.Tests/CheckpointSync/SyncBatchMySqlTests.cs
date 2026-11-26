using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Application.Internal.CommandServices;
using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.CheckpointSync.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.Tests.FieldTracking;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.CheckpointSync;

/// <summary>S T1 and S T4 against a disposable local MySQL database: unique indexes and the single transaction.</summary>
public sealed class SyncBatchMySqlTests
{
    [LocalMySqlFact]
    public async Task CommittedBatchReloadsWithTheSameResultAndTheModelMatchesTheMigrations()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var context = database.Open();
        var sessionId = await SeedSession(context);
        var command = Command(sessionId);

        var created = Success(await Service(context).Handle(command, Actor, default));

        await using var reloaded = database.Open();
        var batch = await new SyncBatchRepository(reloaded).FindByIdAndAgencyIdAsync(created.Id, 1);
        Assert.NotNull(batch);
        Assert.Equal(created.ReceivedAt, batch.ReceivedAt);
        Assert.Equal(created.RecordIdsWith(SyncRecordOutcome.Accepted), batch.RecordIdsWith(SyncRecordOutcome.Accepted));
        Assert.Equal(SyncBatchEvaluationStatus.Pending, batch.EvaluationStatus);
        var reading = await reloaded.Set<TelemetryRecord>().SingleAsync();
        Assert.Equal(command.Readings[0].RecordedAt, reading.RecordedAt);
        Assert.Equal(command.Passages[0].PassedAt, (await reloaded.Set<CheckpointPassage>().SingleAsync()).PassedAt);
        Assert.False(reloaded.Database.HasPendingModelChanges());
    }

    [LocalMySqlFact]
    public async Task ConcurrentIdenticalBatchesCommitOnceAndReturnTheSameBatch()
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

        var command = Command(sessionId);
        var results = await Task.WhenAll(
            Service(firstContext, new GatedUnitOfWork(new UnitOfWork(firstContext), BeforeSave)).Handle(command, Actor, default),
            Service(secondContext, new GatedUnitOfWork(new UnitOfWork(secondContext), BeforeSave)).Handle(command, Actor, default));

        var batches = results.Select(Success).ToArray();
        Assert.Equal(batches[0].Id, batches[1].Id);
        Assert.Equal(batches[0].RecordIdsWith(SyncRecordOutcome.Accepted), batches[1].RecordIdsWith(SyncRecordOutcome.Accepted));
        await using var persisted = database.Open();
        Assert.Single(await persisted.Set<SyncBatch>().ToListAsync());
        Assert.Single(await persisted.Set<TelemetryRecord>().ToListAsync());
        Assert.Single(await persisted.Set<CheckpointPassage>().ToListAsync());
    }

    [LocalMySqlFact]
    public async Task FailureBeforeCommitLeavesNoBatchReadingsOrPassages()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var setup = database.Open();
        var sessionId = await SeedSession(setup);
        var fault = new FailingCommit();
        await using var context = database.Open(fault);

        var result = await Service(context).Handle(Command(sessionId), Actor, default);

        Assert.True(fault.Reached);
        Assert.Equal(ESubmitSyncBatchError.UnexpectedError,
            Assert.IsType<Result<SyncBatch, ESubmitSyncBatchError>.Failure>(result).Error);
        Assert.Empty(context.ChangeTracker.Entries());
        await using var persisted = database.Open();
        Assert.Empty(await persisted.Set<SyncBatch>().ToListAsync());
        Assert.Empty(await persisted.Set<TelemetryRecord>().ToListAsync());
        Assert.Empty(await persisted.Set<CheckpointPassage>().ToListAsync());
    }

    private static readonly VitalTrek.API.IAM.Interfaces.ACL.Resources.AuthenticatedActor Actor = new TrackingStartData().Actor;

    private static SubmitSyncBatchCommand Command(int sessionId)
    {
        var capturedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds());
        return new SubmitSyncBatchCommand(1, 10, Guid.NewGuid(), sessionId, 30,
            [new SyncBatchReadingEntry(Guid.NewGuid(), 70, capturedAt, -9.543212345, -77.543212345, 3020.125, 92, 75, 45.25)],
            [new SyncBatchPassageEntry(Guid.NewGuid(), 30, capturedAt.AddMinutes(1))]);
    }

    private static SyncBatch Success(Result<SyncBatch, ESubmitSyncBatchError> result) =>
        Assert.IsType<Result<SyncBatch, ESubmitSyncBatchError>.Success>(result).Value;

    private static async Task<int> SeedSession(AppDbContext context)
    {
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        (await context.Set<ExpeditionGroup>().SingleAsync()).Start(startedAt);
        var session = new TrackingSession(new StartTrackingSessionCommand(1, 101), startedAt);
        context.Add(session);
        await new UnitOfWork(context).CompleteAsync();
        return session.Id;
    }

    private static SyncBatchCommandService Service(AppDbContext context, IUnitOfWork? unitOfWork = null)
    {
        var sessions = new TrackingSessionRepository(context);
        var expedition = new ExpeditionSetupContextFacade(new FieldGuideRepository(context),
            new ManifestEntryRepository(context), new ExpeditionGroupRepository(context), new CheckpointRepository(context),
            new ExpectedTimeWindowRepository(context), InterfaceStub<IExpeditionStartService>.Create(),
            new ParticipantPreparationRepository(context), new WearableDeviceRepository(context));
        var work = unitOfWork ?? new UnitOfWork(context);
        var telemetry = new TelemetryRecordCommandService(sessions, new TelemetryRecordRepository(context), expedition,
            work, NullLogger<TelemetryRecordCommandService>.Instance);
        var external = new ExternalFieldTrackingService(new FieldTrackingContextFacade(sessions, expedition, telemetry),
            expedition);
        var passages = new CheckpointPassageCommandService(new CheckpointPassageRepository(context), external, work,
            NullLogger<CheckpointPassageCommandService>.Instance);
        return new SyncBatchCommandService(new SyncBatchRepository(context), passages, external, work,
            NullLogger<SyncBatchCommandService>.Instance);
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
            throw new InvalidOperationException("Test failure before the sync batch transaction commit.");
        }
    }
}
