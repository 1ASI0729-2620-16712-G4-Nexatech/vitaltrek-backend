using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="ManifestEntry"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class ManifestEntryRepository(AppDbContext context) : BaseRepository<ManifestEntry>(context), IManifestEntryRepository
{
    private IQueryable<ManifestEntry> InScope(AccessScope scope)
    {
        var groups = Context.Set<ExpeditionGroup>();
        return Context.Set<ManifestEntry>().Where(entry => entry.AgencyId == scope.AgencyId &&
            (scope.FieldGuideId == null || groups.Any(group =>
                group.Id == entry.ExpeditionGroupId && group.FieldGuideId == scope.FieldGuideId)) &&
            (scope.IdentityDocument == null || entry.IdentityDocument == scope.IdentityDocument));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ManifestEntry>> FindByScopeAsync(AccessScope scope, int? expeditionGroupId,
        CancellationToken cancellationToken = default) =>
        await InScope(scope).Where(entry => expeditionGroupId == null || entry.ExpeditionGroupId == expeditionGroupId)
            .OrderBy(entry => entry.ExpeditionGroupId).ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ManifestEntry?> FindByIdAndScopeAsync(int manifestEntryId, AccessScope scope,
        CancellationToken cancellationToken = default) =>
        await InScope(scope).FirstOrDefaultAsync(entry => entry.Id == manifestEntryId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountByExpeditionGroupIdAsync(int expeditionGroupId, CancellationToken cancellationToken = default) =>
        await Context.Set<ManifestEntry>().CountAsync(entry => entry.ExpeditionGroupId == expeditionGroupId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByExpeditionGroupIdAndIdentityDocumentAsync(int expeditionGroupId, string identityDocument,
        CancellationToken cancellationToken = default)
    {
        var document = identityDocument.Trim();
        return await Context.Set<ManifestEntry>().AnyAsync(entry =>
            entry.ExpeditionGroupId == expeditionGroupId && entry.IdentityDocument == document, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsOnDepartureDateAsync(int agencyId, string identityDocument, DateOnly departureDate,
        int excludedExpeditionGroupId, CancellationToken cancellationToken = default)
    {
        var document = identityDocument.Trim();
        var groups = Context.Set<ExpeditionGroup>();
        return await Context.Set<ManifestEntry>().AnyAsync(entry => entry.AgencyId == agencyId &&
            entry.IdentityDocument == document && entry.ExpeditionGroupId != excludedExpeditionGroupId &&
            groups.Any(group => group.Id == entry.ExpeditionGroupId && group.DepartureDate == departureDate),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByAgencyIdAndIdentityDocumentAsync(int agencyId, string identityDocument,
        CancellationToken cancellationToken = default)
    {
        var document = identityDocument.Trim();
        return await Context.Set<ManifestEntry>().AnyAsync(entry =>
            entry.AgencyId == agencyId && entry.IdentityDocument == document, cancellationToken);
    }
}
