using VitalTrek.API.FieldTracking.Interfaces.ACL;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that validates tracking session information through the Field Tracking facade.
/// </summary>
/// <param name="fieldTrackingContextFacade">The Field Tracking facade.</param>
public class ExternalTrackingSessionService(IFieldTrackingContextFacade fieldTrackingContextFacade)
{
    /// <summary>Checks whether a tracking session exists in an agency.</summary>
    public async Task<bool> ExistsSession(int agencyId, int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.SessionExistsInAgencyAsync(agencyId, sessionId, cancellationToken);

    /// <summary>Checks whether a tracking session is active.</summary>
    public async Task<bool> IsSessionActive(int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.IsSessionActiveAsync(sessionId, cancellationToken);

    /// <summary>Finds the expedition group tracked by a session.</summary>
    public async Task<int?> FindExpeditionGroupId(int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync(sessionId, cancellationToken);
}
