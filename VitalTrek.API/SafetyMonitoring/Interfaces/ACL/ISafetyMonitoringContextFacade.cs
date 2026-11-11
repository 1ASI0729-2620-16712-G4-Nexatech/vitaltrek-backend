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
}
