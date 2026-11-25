using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;

/// <summary>Prepares the group transition; Field Tracking saves it together with the session.</summary>
public class ExpeditionStartService(
    IExpeditionGroupRepository groupRepository,
    IFieldGuideRepository guideRepository,
    IRouteRepository routeRepository,
    ICheckpointRepository checkpointRepository,
    IExpectedTimeWindowRepository windowRepository,
    ISafetyProtocolRepository protocolRepository,
    IManifestEntryRepository manifestRepository,
    IParticipantPreparationRepository preparationRepository,
    IWearableDeviceRepository wearableRepository) : IExpeditionStartService
{
    public async Task<ExpeditionStartStatus> StageStart(AuthenticatedActor actor, int expeditionGroupId,
        DateTimeOffset startedAt, CancellationToken cancellationToken = default)
    {
        if (actor.UserId <= 0 || actor.AgencyId <= 0 || actor.RoleCode != "field_guide" || actor.FieldGuideId is not > 0)
            return ExpeditionStartStatus.GroupNotFound;

        var scope = new AccessScope(actor.AgencyId, actor.FieldGuideId);
        var group = await groupRepository.FindByIdAndScopeAsync(expeditionGroupId, scope, cancellationToken);
        if (group is null || await guideRepository.FindByIdAndAgencyIdAsync(
                actor.FieldGuideId.Value, actor.AgencyId, cancellationToken) is null)
            return ExpeditionStartStatus.GroupNotFound;
        if (!group.IsScheduled) return ExpeditionStartStatus.GroupNotScheduled;

        var route = await routeRepository.FindByIdAndAgencyIdAsync(group.RouteId, actor.AgencyId, cancellationToken);
        if (route is not { IsEnabled: true }) return ExpeditionStartStatus.RouteNotReady;
        var checkpoints = (await checkpointRepository.FindByAgencyIdAsync(actor.AgencyId, group.RouteId,
            cancellationToken)).OrderBy(checkpoint => checkpoint.SequenceOrder).ToArray();
        if (checkpoints.Length < 2) return ExpeditionStartStatus.RouteNotReady;
        var windows = (await windowRepository.FindByAgencyIdAsync(actor.AgencyId, group.RouteId,
            cancellationToken)).ToArray();
        for (var index = 0; index < checkpoints.Length - 1; index++)
            if (!windows.Any(window => window.FromCheckpointId == checkpoints[index].Id &&
                    window.ToCheckpointId == checkpoints[index + 1].Id &&
                    window.MinimumMinutes >= 0 && window.MaximumMinutes > window.MinimumMinutes))
                return ExpeditionStartStatus.RouteNotReady;

        var protocols = await protocolRepository.FindByAgencyIdAsync(actor.AgencyId, group.RouteId, cancellationToken);
        if (!protocols.Any(protocol => protocol.Status == SafetyProtocolStatus.Active &&
                !string.IsNullOrWhiteSpace(protocol.Instructions)))
            return ExpeditionStartStatus.ProtocolNotReady;

        var manifest = (await manifestRepository.FindByScopeAsync(scope, group.Id, cancellationToken)).ToArray();
        if (manifest.Length == 0 || manifest.Length > group.MaximumCapacity)
            return ExpeditionStartStatus.ParticipantsNotReady;
        var preparations = (await preparationRepository.FindByScopeAsync(scope, group.Id, cancellationToken)).ToArray();
        var usedWearables = new HashSet<int>();
        foreach (var member in manifest)
        {
            var matches = preparations.Where(preparation => preparation.MemberId == member.Id).ToArray();
            if (matches.Length != 1 || !matches[0].IsReady || matches[0].ReturnedAt is not null ||
                matches[0].ConsentRecordedAt is null || !usedWearables.Add(matches[0].WearableId!.Value))
                return ExpeditionStartStatus.ParticipantsNotReady;
            var wearable = await wearableRepository.FindByIdAndAgencyIdAsync(matches[0].WearableId!.Value,
                actor.AgencyId, cancellationToken);
            if (wearable?.Status != WearableDeviceStatus.Assigned ||
                await preparationRepository.IsWearableInUseAsync(wearable.Id, matches[0].Id, cancellationToken))
                return ExpeditionStartStatus.ParticipantsNotReady;
        }

        group.Start(startedAt);
        return ExpeditionStartStatus.Ready;
    }
}
