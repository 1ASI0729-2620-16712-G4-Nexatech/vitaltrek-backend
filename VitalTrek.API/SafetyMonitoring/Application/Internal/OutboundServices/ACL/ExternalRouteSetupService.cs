using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that reads the route configuration of an expedition group through the Expedition Setup facade.
/// </summary>
/// <param name="expeditionSetupContextFacade">The Expedition Setup facade.</param>
public class ExternalRouteSetupService(IExpeditionSetupContextFacade expeditionSetupContextFacade)
{
    /// <summary>Checks whether a checkpoint belongs to the route of an expedition group.</summary>
    public async Task<bool> ExistsCheckpointInGroupRoute(int agencyId, int expeditionGroupId, int checkpointId,
        CancellationToken cancellationToken) =>
        await expeditionSetupContextFacade.ExistsCheckpointInExpeditionGroupRoute(agencyId, expeditionGroupId,
            checkpointId, cancellationToken);

    /// <summary>Returns the expected time windows of the route of an expedition group.</summary>
    public async Task<IReadOnlyList<(int FromCheckpointId, int ToCheckpointId, int MaximumMinutes)>> FindExpectedTimeWindows(
        int agencyId, int expeditionGroupId, CancellationToken cancellationToken) =>
        await expeditionSetupContextFacade.FindExpectedTimeWindowsByExpeditionGroup(agencyId, expeditionGroupId,
            cancellationToken);

    /// <summary>
    ///     Returns the heart rate baseline of each participant of an expedition group of an agency, by manifest entry.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, (int BaselineMin, int BaselineMax)>> FindParticipantBaselines(int agencyId,
        int expeditionGroupId, CancellationToken cancellationToken) =>
        (await expeditionSetupContextFacade.FindParticipantBaselinesByExpeditionGroup(agencyId, expeditionGroupId,
            cancellationToken))
        .GroupBy(baseline => baseline.MemberId)
        .ToDictionary(group => group.Key, group => (group.First().BaselineMin, group.First().BaselineMax));
}
