using Microsoft.Extensions.Options;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.SafetyMonitoring.Infrastructure.Scheduling.Configuration;

namespace VitalTrek.API.SafetyMonitoring.Infrastructure.Scheduling.Services;

/// <summary>
///     Background service that runs the periodic safety evaluation (D02). Each cycle first recovers the sync batches
///     whose evaluation is still pending, so a failure or a restart never loses the need to evaluate accepted readings,
///     and then evaluates the delay of every active session. Every item runs in its own scope and unit of work, so a
///     failed item leaves nothing staged for the next one. The evaluations are idempotent, so running a cycle twice
///     never duplicates an alert.
/// </summary>
/// <param name="scopeFactory">The factory of the scope of each item.</param>
/// <param name="timeProvider">The clock that drives the cycles.</param>
/// <param name="settings">The periodic evaluation settings.</param>
/// <param name="logger">Logger for diagnostics.</param>
public sealed class SafetyEvaluationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<SafetyEvaluationSettings> settings,
    ILogger<SafetyEvaluationWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.Enabled)
        {
            logger.LogInformation("Periodic safety evaluation is disabled");
            return;
        }

        using var timer = new PeriodicTimer(settings.Value.Interval, timeProvider);
        try
        {
            // The first cycle runs at start, so batches left pending by a restart are recovered immediately.
            do
            {
                await RunCycleAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping.
        }
    }

    /// <summary>
    ///     Runs one evaluation cycle: pending batches first, then the delays of the active sessions.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The outcome of the cycle.</returns>
    public async Task<SafetyEvaluationCycleResult> RunCycleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PendingSyncBatch> batches;
        IReadOnlyList<ActiveSession> sessions;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<ISafetyEvaluationWorkQueryService>();
            var limit = settings.Value.ItemsPerCycle;
            batches = await queries.Handle(new GetPendingSyncBatchEvaluationsQuery(limit), cancellationToken);
            sessions = await queries.Handle(new GetActiveTrackingSessionsQuery(limit), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Safety evaluation cycle could not list its pending work; it is retried next cycle");
            return new SafetyEvaluationCycleResult(0, 0, 0, 0);
        }

        var (evaluatedBatches, failedBatches) = (0, 0);
        foreach (var batch in batches)
        {
            var evaluated = await RunItemAsync(service => service.Handle(
                    new EvaluateSyncBatchCommand(batch.AgencyId, batch.BatchId), cancellationToken),
                result => result.IsSuccess, "sync batch", batch.BatchId, cancellationToken);
            if (evaluated) evaluatedBatches++;
            else failedBatches++;
        }

        var (evaluatedSessions, failedSessions) = (0, 0);
        foreach (var session in sessions)
        {
            var evaluated = await RunItemAsync(service => service.Handle(
                    new EvaluateSessionDelaysCommand(session.AgencyId, session.SessionId), cancellationToken),
                result => result.IsSuccess, "session delays", session.SessionId, cancellationToken);
            if (evaluated) evaluatedSessions++;
            else failedSessions++;
        }

        if (failedBatches + failedSessions > 0)
            logger.LogWarning("Safety evaluation cycle left {FailedBatches} batches and {FailedSessions} sessions for a retry",
                failedBatches, failedSessions);
        return new SafetyEvaluationCycleResult(evaluatedBatches, failedBatches, evaluatedSessions, failedSessions);
    }

    private async Task<bool> RunItemAsync<TResult>(Func<IEarlyWarningAlertCommandService, Task<TResult>> evaluate,
        Func<TResult, bool> succeeded, string item, int itemId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEarlyWarningAlertCommandService>();
            return succeeded(await evaluate(service));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error evaluating {Item} {ItemId}; it is retried next cycle", item, itemId);
            return false;
        }
    }
}

/// <summary>Outcome of one periodic safety evaluation cycle.</summary>
/// <param name="EvaluatedBatches">Pending batches evaluated in this cycle.</param>
/// <param name="FailedBatches">Pending batches left for the next cycle.</param>
/// <param name="EvaluatedSessions">Active sessions whose delays were evaluated.</param>
/// <param name="FailedSessions">Active sessions left for the next cycle.</param>
public sealed record SafetyEvaluationCycleResult(int EvaluatedBatches, int FailedBatches, int EvaluatedSessions,
    int FailedSessions);
