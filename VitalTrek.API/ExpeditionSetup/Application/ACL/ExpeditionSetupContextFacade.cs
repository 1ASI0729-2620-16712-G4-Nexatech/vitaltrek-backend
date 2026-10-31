using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;

namespace VitalTrek.API.ExpeditionSetup.Application.ACL;

/// <summary>
///     Implementation of <see cref="IExpeditionSetupContextFacade"/> backed by the Expedition Setup repositories.
/// </summary>
/// <param name="fieldGuideRepository">The field guide repository.</param>
/// <param name="manifestEntryRepository">The manifest entry repository.</param>
public class ExpeditionSetupContextFacade(
    IFieldGuideRepository fieldGuideRepository,
    IManifestEntryRepository manifestEntryRepository) : IExpeditionSetupContextFacade
{
    /// <inheritdoc />
    public async Task<bool> ExistsFieldGuide(int agencyId, int fieldGuideId, CancellationToken cancellationToken = default) =>
        await fieldGuideRepository.FindByIdAndAgencyIdAsync(fieldGuideId, agencyId, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<bool> ExistsManifestIdentityDocument(int agencyId, string identityDocument,
        CancellationToken cancellationToken = default) =>
        !string.IsNullOrWhiteSpace(identityDocument) &&
        await manifestEntryRepository.ExistsByAgencyIdAndIdentityDocumentAsync(agencyId, identityDocument, cancellationToken);
}
