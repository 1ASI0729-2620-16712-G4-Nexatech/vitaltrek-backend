using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;

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
    IExpectedTimeWindowRepository expectedTimeWindowRepository) : IExpeditionSetupContextFacade
{
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
