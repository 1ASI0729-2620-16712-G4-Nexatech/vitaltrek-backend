using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that resolves the expedition group scope of the authenticated actor through the Expedition Setup
///     facade: administrators see the groups of their agency and field guides their assigned groups.
/// </summary>
/// <param name="expeditionSetupContextFacade">The Expedition Setup facade.</param>
public class ExternalExpeditionSetupService(IExpeditionSetupContextFacade expeditionSetupContextFacade)
{
    /// <summary>Checks whether the actor can see an expedition group.</summary>
    public async Task<bool> CanAccessGroup(AuthenticatedActor actor, int expeditionGroupId,
        CancellationToken cancellationToken) =>
        expeditionGroupId > 0 &&
        await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor, expeditionGroupId, cancellationToken);

    /// <summary>Returns the expedition groups the actor can see.</summary>
    public async Task<IReadOnlyCollection<int>> FindAccessibleGroupIds(AuthenticatedActor actor,
        CancellationToken cancellationToken) =>
        await expeditionSetupContextFacade.FindAccessibleExpeditionGroupIds(actor, cancellationToken);
}
