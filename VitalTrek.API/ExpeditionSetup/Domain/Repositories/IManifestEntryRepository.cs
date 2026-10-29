using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="ManifestEntry"/> aggregates.
/// </summary>
public interface IManifestEntryRepository : IBaseRepository<ManifestEntry>
{
    /// <summary>Finds the manifest entries visible in an access scope, optionally filtered by group.</summary>
    Task<IEnumerable<ManifestEntry>> FindByScopeAsync(AccessScope scope, int? expeditionGroupId, CancellationToken cancellationToken = default);

    /// <summary>Finds a manifest entry visible in an access scope.</summary>
    Task<ManifestEntry?> FindByIdAndScopeAsync(int manifestEntryId, AccessScope scope, CancellationToken cancellationToken = default);

    /// <summary>Counts the participants of a group.</summary>
    Task<int> CountByExpeditionGroupIdAsync(int expeditionGroupId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an identity document is already registered in a group.</summary>
    Task<bool> ExistsByExpeditionGroupIdAndIdentityDocumentAsync(int expeditionGroupId, string identityDocument,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether an identity document is registered in another group of the agency with the same departure date.</summary>
    Task<bool> ExistsOnDepartureDateAsync(int agencyId, string identityDocument, DateOnly departureDate,
        int excludedExpeditionGroupId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an identity document appears in any manifest of the agency.</summary>
    Task<bool> ExistsByAgencyIdAndIdentityDocumentAsync(int agencyId, string identityDocument, CancellationToken cancellationToken = default);
}
