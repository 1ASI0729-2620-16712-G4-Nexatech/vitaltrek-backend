using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;

namespace VitalTrek.API.ExpeditionSetup.Application.ACL;

/// <summary>
///     Implementation of <see cref="IExpeditionSetupContextFacade"/> backed by the Expedition Setup repositories.
/// </summary>
/// <param name="fieldGuideRepository">The field guide repository.</param>
/// <param name="manifestEntryRepository">The manifest entry repository.</param>
/// <param name="expeditionGroupRepository">The expedition group repository.</param>
/// <param name="checkpointRepository">The checkpoint repository.</param>
/// <param name="expectedTimeWindowRepository">The expected time window repository.</param>
public class ExpeditionSetupContextFacade(
    IFieldGuideRepository fieldGuideRepository,
    IManifestEntryRepository manifestEntryRepository,
    IExpeditionGroupRepository expeditionGroupRepository,
    ICheckpointRepository checkpointRepository,
    IExpectedTimeWindowRepository expectedTimeWindowRepository,
    IExpeditionStartService expeditionStartService,
    IParticipantPreparationRepository participantPreparationRepository,
    IWearableDeviceRepository wearableDeviceRepository) : IExpeditionSetupContextFacade
{
    /// <inheritdoc />
    public async Task<TrackingParticipantPreparation?> FindTrackingPreparation(AuthenticatedActor actor,
        int expeditionGroupId, int wearableDeviceId, CancellationToken cancellationToken = default)
    {
        var scope = ToScope(actor);
        if (scope is null || wearableDeviceId <= 0 ||
            !await CanAccessExpeditionGroup(actor, expeditionGroupId, cancellationToken)) return null;
        var preparations = await participantPreparationRepository.FindByScopeAsync(scope, expeditionGroupId, cancellationToken);
        var matches = preparations.Where(preparation => preparation.WearableId == wearableDeviceId &&
            preparation.ReturnedAt is null).Take(2).ToArray();
        if (matches.Length != 1 || await participantPreparationRepository.IsWearableInUseAsync(
                wearableDeviceId, matches[0].Id, cancellationToken)) return null;
        var member = await manifestEntryRepository.FindByIdAndScopeAsync(matches[0].MemberId, scope, cancellationToken);
        if (member is null || member.ExpeditionGroupId != expeditionGroupId) return null;
        var wearable = await wearableDeviceRepository.FindByIdAndAgencyIdAsync(wearableDeviceId, actor.AgencyId,
            cancellationToken);
        return new TrackingParticipantPreparation(member.Id, matches[0].Consent, matches[0].Activated,
            wearable?.Status == WearableDeviceStatus.Assigned);
    }

    /// <inheritdoc />
    public Task<ExpeditionStartStatus> StageExpeditionStart(AuthenticatedActor actor, int expeditionGroupId,
        DateTimeOffset startedAt, CancellationToken cancellationToken = default) =>
        expeditionStartService.StageStart(actor, expeditionGroupId, startedAt, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> CanAccessExpeditionGroup(AuthenticatedActor actor, int expeditionGroupId,
        CancellationToken cancellationToken = default)
    {
        var scope = ToScope(actor);
        return scope is not null && expeditionGroupId > 0 &&
               await expeditionGroupRepository.FindByIdAndScopeAsync(expeditionGroupId, scope, cancellationToken)
               is not null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<int>> FindAccessibleExpeditionGroupIds(AuthenticatedActor actor,
        CancellationToken cancellationToken = default)
    {
        var scope = ToScope(actor);
        if (scope is null) return [];
        var groups = await expeditionGroupRepository.FindByScopeAsync(scope, null, cancellationToken);
        return groups.Select(group => group.Id).ToArray();
    }

    private static AccessScope? ToScope(AuthenticatedActor actor)
    {
        if (actor.UserId <= 0 || actor.AgencyId <= 0) return null;
        return actor.RoleCode switch
        {
            "operations_administrator" => new AccessScope(actor.AgencyId),
            "field_guide" when actor.FieldGuideId is > 0 => new AccessScope(actor.AgencyId, actor.FieldGuideId),
            "adventure_tourist" when !string.IsNullOrWhiteSpace(actor.IdentityDocument) =>
                new AccessScope(actor.AgencyId, IdentityDocument: actor.IdentityDocument),
            _ => null
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(int FromCheckpointId, int ToCheckpointId, int MaximumMinutes)>>
        FindExpectedTimeWindowsByExpeditionGroup(int agencyId, int expeditionGroupId,
            CancellationToken cancellationToken = default)
    {
        var group = await expeditionGroupRepository.FindByIdAndScopeAsync(expeditionGroupId, new AccessScope(agencyId),
            cancellationToken);
        if (group is null) return [];
        var windows = await expectedTimeWindowRepository.FindByAgencyIdAsync(agencyId, group.RouteId, cancellationToken);
        return windows.Select(window => (window.FromCheckpointId, window.ToCheckpointId, window.MaximumMinutes)).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsCheckpointInExpeditionGroupRoute(int agencyId, int expeditionGroupId, int checkpointId,
        CancellationToken cancellationToken = default)
    {
        var group = await expeditionGroupRepository.FindByIdAndScopeAsync(expeditionGroupId, new AccessScope(agencyId),
            cancellationToken);
        if (group is null) return false;
        var checkpoint = await checkpointRepository.FindByIdAndAgencyIdAsync(checkpointId, agencyId, cancellationToken);
        return checkpoint is not null && checkpoint.RouteId == group.RouteId;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsFieldGuide(int agencyId, int fieldGuideId, CancellationToken cancellationToken = default) =>
        await fieldGuideRepository.FindByIdAndAgencyIdAsync(fieldGuideId, agencyId, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<bool> ExistsManifestIdentityDocument(int agencyId, string identityDocument,
        CancellationToken cancellationToken = default) =>
        !string.IsNullOrWhiteSpace(identityDocument) &&
        await manifestEntryRepository.ExistsByAgencyIdAndIdentityDocumentAsync(agencyId, identityDocument, cancellationToken);
}
