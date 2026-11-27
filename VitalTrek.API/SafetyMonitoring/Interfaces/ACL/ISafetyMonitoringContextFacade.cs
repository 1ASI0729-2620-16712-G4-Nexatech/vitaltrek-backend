namespace VitalTrek.API.SafetyMonitoring.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes Safety Monitoring information to other bounded contexts
///     without sharing the Safety Monitoring domain model.
/// </summary>
public interface ISafetyMonitoringContextFacade
{
    /// <summary>
    ///     Checks whether a tracking session has open (active or acknowledged) early warning alerts.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when at least one alert is open.</returns>
    Task<bool> HasActiveAlertsForSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts the critical early warning alerts of a tracking session that were not dismissed.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The number of critical alerts.</returns>
    Task<int> CountCriticalAlertsBySessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Evaluates, on the server, the readings accepted by a persisted sync batch of an agency and raises the
    ///     corresponding early warning alerts. The evaluation is idempotent: an evaluated batch is not evaluated again
    ///     and an evidence never raises a second alert. It saves its own unit of work, so callers invoke it after their
    ///     own commit, from a scope without unrelated pending changes.
    /// </summary>
    /// <param name="agencyId">The agency that owns the batch.</param>
    /// <param name="batchId">The sync batch identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     <see langword="true"/> when the batch is evaluated, now or before; <see langword="false"/> when it does not
    ///     exist in the agency or its evaluation stays pending for a retry.
    /// </returns>
    Task<bool> EvaluateSyncBatchAsync(int agencyId, int batchId, CancellationToken cancellationToken = default);
}
