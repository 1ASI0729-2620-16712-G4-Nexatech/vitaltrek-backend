using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.IncidentResponse.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="EvacuationAction"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class EvacuationActionRepository(AppDbContext context)
    : BaseRepository<EvacuationAction>(context), IEvacuationActionRepository
{
    /// <inheritdoc />
    public async Task<EvacuationAction?> FindByClientActionIdAsync(int agencyId, Guid clientActionId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<EvacuationAction>().FirstOrDefaultAsync(
            action => action.AgencyId == agencyId && action.ClientActionId == clientActionId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<EvacuationAction>> FindByIncidentIdAsync(int agencyId, int incidentId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<EvacuationAction>()
            .Where(action => action.AgencyId == agencyId && action.IncidentId == incidentId)
            .OrderBy(action => action.RecordedAt).ThenBy(action => action.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountByIncidentIdsAsync(int agencyId, IReadOnlyCollection<int> incidentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = incidentIds.ToArray();
        return await Context.Set<EvacuationAction>()
            .CountAsync(action => action.AgencyId == agencyId && ids.Contains(action.IncidentId), cancellationToken);
    }
}
