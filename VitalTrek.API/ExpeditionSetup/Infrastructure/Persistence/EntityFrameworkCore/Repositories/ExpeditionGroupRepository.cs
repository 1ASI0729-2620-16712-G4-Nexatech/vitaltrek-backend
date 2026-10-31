using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="ExpeditionGroup"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class ExpeditionGroupRepository(AppDbContext context)
    : BaseRepository<ExpeditionGroup>(context), IExpeditionGroupRepository
{
    private IQueryable<ExpeditionGroup> InScope(AccessScope scope)
    {
        var manifestEntries = Context.Set<ManifestEntry>();
        return Context.Set<ExpeditionGroup>().Where(group => group.AgencyId == scope.AgencyId &&
            (scope.FieldGuideId == null || group.FieldGuideId == scope.FieldGuideId) &&
            (scope.IdentityDocument == null || manifestEntries.Any(entry =>
                entry.ExpeditionGroupId == group.Id && entry.IdentityDocument == scope.IdentityDocument)));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExpeditionGroup>> FindByScopeAsync(AccessScope scope, int? routeId,
        CancellationToken cancellationToken = default) =>
        await InScope(scope).Where(group => routeId == null || group.RouteId == routeId)
            .OrderBy(group => group.DepartureDate).ThenBy(group => group.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ExpeditionGroup?> FindByIdAndScopeAsync(int expeditionGroupId, AccessScope scope,
        CancellationToken cancellationToken = default) =>
        await InScope(scope).FirstOrDefaultAsync(group => group.Id == expeditionGroupId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasFieldGuideScheduleConflictAsync(int fieldGuideId, DateOnly departureDate,
        int excludedExpeditionGroupId, CancellationToken cancellationToken = default) =>
        await Context.Set<ExpeditionGroup>().AnyAsync(group => group.Id != excludedExpeditionGroupId &&
            group.FieldGuideId == fieldGuideId && group.DepartureDate == departureDate &&
            group.Status != ExpeditionGroupStatus.Finished, cancellationToken);
}
