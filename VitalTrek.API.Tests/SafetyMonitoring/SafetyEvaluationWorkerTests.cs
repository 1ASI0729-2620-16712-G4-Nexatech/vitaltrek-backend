using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VitalTrek.API.SafetyMonitoring.Application.Errors;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.SafetyMonitoring.Infrastructure.Scheduling.Configuration;
using VitalTrek.API.SafetyMonitoring.Infrastructure.Scheduling.Services;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring;

/// <summary>
///     D02 and D T4: each cycle recovers the pending batches first and then evaluates the delays of the active sessions,
///     every item in its own scope; a failed item never stops the cycle and the first cycle runs at start.
/// </summary>
public sealed class SafetyEvaluationWorkerTests
{
    private const int ThrowingBatch = 40, FailingBatch = 41;
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

    private readonly ConcurrentQueue<string> _calls = new();
    private IReadOnlyList<PendingSyncBatch> _batches = [];
    private IReadOnlyList<ActiveSession> _sessions = [];
    private bool _listingFails;
    private int _commandScopes;
    private int? _requestedLimit;

    [Fact]
    public async Task CycleRecoversPendingBatchesBeforeTheActiveSessionsEachInItsOwnScope()
    {
        _batches = [new PendingSyncBatch(50, 1), new PendingSyncBatch(51, 2)];
        _sessions = [new ActiveSession(7, 1)];

        var result = await Worker().RunCycleAsync(CancellationToken.None);

        Assert.Equal(new SafetyEvaluationCycleResult(2, 0, 1, 0), result);
        Assert.Equal(new[] { "batch:1:50", "batch:2:51", "session:1:7" }, _calls);
        Assert.Equal(3, _commandScopes);
    }

    [Fact]
    public async Task AFailedItemNeverStopsTheCycle()
    {
        _batches = [new PendingSyncBatch(ThrowingBatch, 1), new PendingSyncBatch(FailingBatch, 1), new PendingSyncBatch(52, 1)];
        _sessions = [new ActiveSession(7, 1)];

        var result = await Worker().RunCycleAsync(CancellationToken.None);

        Assert.Equal(new SafetyEvaluationCycleResult(1, 2, 1, 0), result);
        Assert.Equal(4, _calls.Count);
    }

    [Fact]
    public async Task ListingFailureEndsTheCycleWithoutEvaluatingAndWithoutThrowing()
    {
        _listingFails = true;
        _batches = [new PendingSyncBatch(50, 1)];

        var result = await Worker().RunCycleAsync(CancellationToken.None);

        Assert.Equal(new SafetyEvaluationCycleResult(0, 0, 0, 0), result);
        Assert.Empty(_calls);
    }

    [Fact]
    public async Task ItemsPerCycleStayWithinTheLimitOfTheInternalLookups()
    {
        await Worker(new SafetyEvaluationSettings { MaxItemsPerCycle = 500 }).RunCycleAsync(CancellationToken.None);

        Assert.Equal(100, _requestedLimit);
    }

    [Fact]
    public async Task StartingTheWorkerRecoversPendingBatchesImmediately()
    {
        _batches = [new PendingSyncBatch(50, 1)];
        var worker = Worker();

        await worker.StartAsync(CancellationToken.None);
        for (var attempt = 0; attempt < 250 && _calls.IsEmpty; attempt++) await Task.Delay(20);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(new[] { "batch:1:50" }, _calls);
        Assert.True(worker.ExecuteTask!.IsCompleted);
    }

    [Fact]
    public async Task DisabledWorkerNeverEvaluates()
    {
        _batches = [new PendingSyncBatch(50, 1)];
        var worker = Worker(new SafetyEvaluationSettings { Enabled = false });

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!;
        await worker.StopAsync(CancellationToken.None);

        Assert.Empty(_calls);
        Assert.Null(_requestedLimit);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(60, 60)]
    public void IntervalIsNeverShorterThanTheMinimum(int configuredSeconds, int expectedSeconds)
    {
        var settings = new SafetyEvaluationSettings { EvaluationIntervalSeconds = configuredSeconds };
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), settings.Interval);
    }

    private SafetyEvaluationWorker Worker(SafetyEvaluationSettings? settings = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Queries());
        services.AddScoped(_ =>
        {
            Interlocked.Increment(ref _commandScopes);
            return Commands();
        });
        var provider = services.BuildServiceProvider();
        return new SafetyEvaluationWorker(provider.GetRequiredService<IServiceScopeFactory>(), new FixedTimeProvider(Now),
            Options.Create(settings ?? new SafetyEvaluationSettings()), NullLogger<SafetyEvaluationWorker>.Instance);
    }

    private ISafetyEvaluationWorkQueryService Queries() =>
        InterfaceStub<ISafetyEvaluationWorkQueryService>.Create((_, args) =>
        {
            if (_listingFails) return Task.FromException<IReadOnlyList<PendingSyncBatch>>(new TimeoutException("Database down."));
            switch (args[0])
            {
                case GetPendingSyncBatchEvaluationsQuery query:
                    _requestedLimit = query.MaxCount;
                    return Task.FromResult(_batches);
                case GetActiveTrackingSessionsQuery:
                    return Task.FromResult(_sessions);
                default:
                    throw new InvalidOperationException(args[0]?.GetType().Name);
            }
        });

    private IEarlyWarningAlertCommandService Commands() =>
        InterfaceStub<IEarlyWarningAlertCommandService>.Create((_, args) =>
        {
            switch (args[0])
            {
                case EvaluateSyncBatchCommand batch:
                    _calls.Enqueue($"batch:{batch.AgencyId}:{batch.BatchId}");
                    return batch.BatchId switch
                    {
                        ThrowingBatch => Task.FromException<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>>(
                            new InvalidOperationException("Simulated failure.")),
                        FailingBatch => Task.FromResult<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>>(
                            new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>.Failure(
                                EEvaluateSyncBatchError.UnexpectedError)),
                        _ => Task.FromResult<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>>(
                            new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>.Success([]))
                    };
                case EvaluateSessionDelaysCommand session:
                    _calls.Enqueue($"session:{session.AgencyId}:{session.SessionId}");
                    return Task.FromResult<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>>(
                        new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>.Success([]));
                default:
                    throw new InvalidOperationException(args[0]?.GetType().Name);
            }
        });
}
