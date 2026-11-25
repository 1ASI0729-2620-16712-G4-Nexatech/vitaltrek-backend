using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;
using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

/// <summary>Opt-in tests against disposable local MySQL 8 databases; never uses the application's database.</summary>
public sealed class TrackingSessionMySqlTests
{
    [LocalMySqlFact]
    public async Task MigrationFromPreviousVersionPreservesRowsAndMatchesTheRuntimeModel()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync(previousVersion: true);
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Single(await context.Set<ExpeditionGroup>().ToListAsync());
        Assert.Empty(await context.Set<TrackingSession>().ToListAsync());
    }

    [LocalMySqlFact]
    public async Task ConcurrentStartsCommitExactlyOneSessionAndReturnTheSameIdentityAndTime()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var firstContext = database.Open();
        await using var secondContext = database.Open();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        async Task MeetBeforeSave()
        {
            if (Interlocked.Increment(ref arrivals) == 2) gate.TrySetResult();
            await gate.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        var first = StartService(firstContext, new AwaitingUnitOfWork(new UnitOfWork(firstContext), MeetBeforeSave));
        var second = StartService(secondContext, new AwaitingUnitOfWork(new UnitOfWork(secondContext), MeetBeforeSave));
        var actor = new TrackingStartData().Actor;
        var results = await Task.WhenAll(
            first.Handle(new StartTrackingSessionCommand(1, 101), actor, default),
            second.Handle(new StartTrackingSessionCommand(1, 101), actor, default));
        var starts = results.Select(result => Assert.IsType<Result<TrackingSessionStartResult,
            EStartTrackingSessionError>.Success>(result).Value).ToArray();
        Assert.Single(starts.Where(start => start.WasCreated));
        Assert.Equal(starts[0].Session.Id, starts[1].Session.Id);
        Assert.Equal(starts[0].Session.StartedAt, starts[1].Session.StartedAt);
        await using var persisted = database.Open();
        var session = Assert.Single(await persisted.Set<TrackingSession>().ToListAsync());
        var group = Assert.Single(await persisted.Set<ExpeditionGroup>().ToListAsync());
        Assert.False(group.IsScheduled);
        Assert.Equal(group.StartedAt, session.StartedAt);
    }

    [LocalMySqlFact]
    public async Task FailureImmediatelyBeforeCommitRollsBackGroupAndSession()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        var fault = new FailCommitInterceptor();
        await using var context = database.Open(fault);
        var result = await StartService(context).Handle(new StartTrackingSessionCommand(1, 101),
            new TrackingStartData().Actor, default);
        Assert.True(fault.WasReached);
        Assert.Equal(EStartTrackingSessionError.UnexpectedError,
            Assert.IsType<Result<TrackingSessionStartResult, EStartTrackingSessionError>.Failure>(result).Error);
        Assert.Empty(context.ChangeTracker.Entries());
        await using var persisted = database.Open();
        var group = Assert.Single(await persisted.Set<ExpeditionGroup>().ToListAsync());
        Assert.True(group.IsScheduled);
        Assert.Null(group.StartedAt);
        Assert.Empty(await persisted.Set<TrackingSession>().ToListAsync());
    }

    [LocalMySqlFact]
    public async Task UniqueIndexRejectsASecondActiveSessionButAllowsCompletedHistory()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var context = database.Open();
        context.Add(new TrackingSession(new StartTrackingSessionCommand(1, 101)));
        await context.SaveChangesAsync();
        context.Add(new TrackingSession(new StartTrackingSessionCommand(1, 101)));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        var original = await context.Set<TrackingSession>().SingleAsync();
        original.End();
        await context.SaveChangesAsync();
        context.Add(new TrackingSession(new StartTrackingSessionCommand(1, 101)));
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.Set<TrackingSession>().CountAsync());
        Assert.Equal(1, await context.Set<TrackingSession>().CountAsync(session => session.Status == SessionStatus.Active));
    }

    [LocalMySqlFact]
    public async Task DuplicateLegacySessionsAbortTheWholeSchemaChangeWithoutDeletingData()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync(previousVersion: true);
        await using var context = database.Open();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO tracking_sessions (agency_id, expedition_group_id, started_at, status)
            VALUES (1, 101, UTC_TIMESTAMP(), 'active'), (1, 101, UTC_TIMESTAMP(), 'active');
            """);
        var error = await Assert.ThrowsAsync<MySqlException>(() => context.Database.MigrateAsync());
        Assert.Equal(1062, error.Number);
        await using var connection = new MySqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var column = new MySqlCommand("""
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = 'tracking_sessions'
                AND column_name = 'active_expedition_group_id';
            """, connection);
        Assert.Equal(0L, Convert.ToInt64(await column.ExecuteScalarAsync()));
        await using var count = new MySqlCommand("SELECT COUNT(*) FROM tracking_sessions;", connection);
        Assert.Equal(2L, Convert.ToInt64(await count.ExecuteScalarAsync()));
    }

    private static TrackingSessionCommandService StartService(AppDbContext context, IUnitOfWork? unitOfWork = null)
    {
        var groups = new ExpeditionGroupRepository(context);
        var guides = new FieldGuideRepository(context);
        var manifest = new ManifestEntryRepository(context);
        var checkpoints = new CheckpointRepository(context);
        var windows = new ExpectedTimeWindowRepository(context);
        var preparation = new ExpeditionStartService(groups, guides, new RouteRepository(context), checkpoints,
            windows, new SafetyProtocolRepository(context), manifest, new ParticipantPreparationRepository(context),
            new WearableDeviceRepository(context));
        var facade = new ExpeditionSetupContextFacade(guides, manifest, groups, checkpoints, windows, preparation);
        return new TrackingSessionCommandService(new TrackingSessionRepository(context), facade,
            unitOfWork ?? new UnitOfWork(context), NullLogger<TrackingSessionCommandService>.Instance);
    }

    private sealed class AwaitingUnitOfWork(IUnitOfWork inner, Func<Task> beforeSave) : IUnitOfWork
    {
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await beforeSave();
            await inner.CompleteAsync(cancellationToken);
        }
        public void DiscardChanges() => inner.DiscardChanges();
    }

    private sealed class FailCommitInterceptor : DbTransactionInterceptor
    {
        public bool WasReached { get; private set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            WasReached = true;
            throw new InvalidOperationException("Injected failure before transaction commit.");
        }
    }
}

internal sealed class LocalMySqlFactAttribute : FactAttribute
{
    public LocalMySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VITALTREK_TEST_MYSQL")))
            Skip = "Set VITALTREK_TEST_MYSQL to a local MySQL 8 server with permission to create disposable databases.";
    }
}

internal sealed class LocalTrackingDatabase : IAsyncDisposable
{
    private const string DatabasePrefix = "vitaltrek_m02_test_";
    private readonly string _name = DatabasePrefix + Guid.NewGuid().ToString("N");
    private readonly string _serverConnection;
    private bool _created;
    public string ConnectionString { get; }

    private LocalTrackingDatabase()
    {
        var settings = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("VITALTREK_TEST_MYSQL"));
        if (settings.Server is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("M02 integration tests only accept a loopback MySQL server.");
        settings.Database = "";
        _serverConnection = settings.ConnectionString;
        settings.Database = _name;
        ConnectionString = settings.ConnectionString;
    }

    public AppDbContext Open(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseMySQL(ConnectionString).AddInterceptors(interceptors).Options);

    public static async Task<LocalTrackingDatabase> CreateAsync(bool previousVersion = false)
    {
        var database = new LocalTrackingDatabase();
        try
        {
            await using var server = new MySqlConnection(database._serverConnection);
            await server.OpenAsync();
            await using var create = new MySqlCommand($"CREATE DATABASE `{database._name}`;", server);
            await create.ExecuteNonQueryAsync();
            database._created = true;
            await using var context = database.Open();
            await context.GetService<IMigrator>().MigrateAsync(previousVersion ? "20261111043327_AddTourClosure" : null);
            var data = new TrackingStartData();
            context.Add(TrackingStartData.WithId(new Agency(new SignUpCommand("Test agency", "Admin",
                new Username("admin@example.test"), "test-only-password")), 1));
            context.Add(data.Guide);
            context.Add(data.Route);
            context.Add(data.Group);
            context.AddRange(data.Checkpoints);
            context.AddRange(data.Windows);
            context.AddRange(data.Protocols);
            context.AddRange(data.Members);
            context.AddRange(data.Devices);
            context.AddRange(data.Preparations);
            await context.SaveChangesAsync();
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_created) return;
        // The name is generated by this instance; never accept a name supplied in the connection string.
        if (!_name.StartsWith(DatabasePrefix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(_name[DatabasePrefix.Length..], "N", out _))
            throw new InvalidOperationException("Invalid disposable database name.");
        MySqlConnection.ClearAllPools();
        await using var server = new MySqlConnection(_serverConnection);
        await server.OpenAsync();
        await using var drop = new MySqlCommand($"DROP DATABASE IF EXISTS `{_name}`;", server);
        await drop.ExecuteNonQueryAsync();
        _created = false;
    }
}
