using VitalTrek.API.SafetyMonitoring.Interfaces.ACL;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL.Resources;

namespace VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that reads early warning alert references through the Safety Monitoring facade. Incident
///     Response never changes the state of an alert.
/// </summary>
/// <param name="safetyMonitoringContextFacade">The Safety Monitoring facade.</param>
public class ExternalSafetyMonitoringService(ISafetyMonitoringContextFacade safetyMonitoringContextFacade)
{
    /// <summary>Status code of a dismissed alert, published by the Safety Monitoring contract.</summary>
    public const string DismissedAlertStatusCode = "dismissed";

    /// <summary>Returns the identifiers of an alert of the agency, or null when it does not exist there.</summary>
    public async Task<AlertReference?> FindAlert(int agencyId, int alertId, CancellationToken cancellationToken) =>
        await safetyMonitoringContextFacade.FindAlertReferenceAsync(agencyId, alertId, cancellationToken);
}
