using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL;

namespace VitalTrek.API.SafetyMonitoring.Application.ACL;

/// <summary>
///     Implementation of <see cref="ISafetyMonitoringContextFacade"/> backed by the Safety Monitoring repositories.
/// </summary>
/// <param name="alertRepository">The early warning alert repository.</param>
public class SafetyMonitoringContextFacade(IEarlyWarningAlertRepository alertRepository) : ISafetyMonitoringContextFacade
{
    /// <inheritdoc />
    public async Task<bool> HasActiveAlertsForSessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await alertRepository.HasOpenBySessionIdAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountCriticalAlertsBySessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await alertRepository.CountCriticalBySessionIdAsync(sessionId, cancellationToken);
}
