using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL;

namespace VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that validates tracking session information through the Field Tracking facade.
///     The route of a session is resolved through its expedition group using the Expedition Setup facade.
/// </summary>
/// <param name="fieldTrackingContextFacade">The Field Tracking facade.</param>
/// <param name="expeditionSetupContextFacade">The Expedition Setup facade.</param>
public class ExternalFieldTrackingService(
    IFieldTrackingContextFacade fieldTrackingContextFacade,
    IExpeditionSetupContextFacade expeditionSetupContextFacade)
{
    /// <summary>Checks whether a tracking session exists in an agency.</summary>
    public async Task<bool> ExistsSession(int agencyId, int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.SessionExistsInAgencyAsync(agencyId, sessionId, cancellationToken);

    /// <summary>Checks whether a tracking session is active.</summary>
    public async Task<bool> IsSessionActive(int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.IsSessionActiveAsync(sessionId, cancellationToken);

    /// <summary>Checks whether a checkpoint belongs to the route of the expedition group tracked by a session.</summary>
    public async Task<bool> ExistsCheckpointInSessionRoute(int agencyId, int sessionId, int checkpointId,
        CancellationToken cancellationToken)
    {
        var groupId = await fieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync(sessionId, cancellationToken);
        return groupId is not null && await expeditionSetupContextFacade.ExistsCheckpointInExpeditionGroupRoute(
            agencyId, groupId.Value, checkpointId, cancellationToken);
    }
}
