using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL.Resources;

namespace VitalTrek.API.SafetyMonitoring.Application.ACL;

/// <summary>
///     Implementation of <see cref="ISafetyMonitoringContextFacade"/> backed by the Safety Monitoring repositories.
/// </summary>
/// <param name="alertRepository">The early warning alert repository.</param>
/// <param name="alertCommandService">The early warning alert command service.</param>
public class SafetyMonitoringContextFacade(
    IEarlyWarningAlertRepository alertRepository,
    IEarlyWarningAlertCommandService alertCommandService) : ISafetyMonitoringContextFacade
{
    /// <inheritdoc />
    public async Task<bool> HasActiveAlertsForSessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await alertRepository.HasOpenBySessionIdAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountCriticalAlertsBySessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await alertRepository.CountCriticalBySessionIdAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> EvaluateSyncBatchAsync(int agencyId, int batchId, CancellationToken cancellationToken = default) =>
        (await alertCommandService.Handle(new EvaluateSyncBatchCommand(agencyId, batchId), cancellationToken)).IsSuccess;

    /// <inheritdoc />
    public async Task<AlertReference?> FindAlertReferenceAsync(int agencyId, int alertId,
        CancellationToken cancellationToken = default) =>
        await alertRepository.FindByIdAndAgencyIdAsync(alertId, agencyId, cancellationToken) is { } alert
            ? new AlertReference(alert.Id, alert.SessionId, alert.ExpeditionGroupId, alert.Status.ToCode())
            : null;
}
