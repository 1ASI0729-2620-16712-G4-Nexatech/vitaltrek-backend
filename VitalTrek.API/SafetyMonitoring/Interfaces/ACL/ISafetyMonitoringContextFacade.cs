using VitalTrek.API.SafetyMonitoring.Interfaces.ACL.Resources;

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

    /// <summary>
    ///     Returns the identifiers and status of an early warning alert of an agency, or null when it does not exist there.
    /// </summary>
    /// <param name="agencyId">The agency of the authenticated user.</param>
    /// <param name="alertId">The alert identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The alert reference, or <see langword="null"/>.</returns>
    Task<AlertReference?> FindAlertReferenceAsync(int agencyId, int alertId, CancellationToken cancellationToken = default);
}
