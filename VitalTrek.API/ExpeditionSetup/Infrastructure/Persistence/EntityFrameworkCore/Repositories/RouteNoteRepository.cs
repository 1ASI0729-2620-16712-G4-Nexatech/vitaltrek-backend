using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="RouteNote"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class RouteNoteRepository(AppDbContext context) : BaseRepository<RouteNote>(context), IRouteNoteRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<RouteNote>> FindByAgencyIdAsync(int agencyId, int? routeId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<RouteNote>()
            .Where(note => note.AgencyId == agencyId && (routeId == null || note.RouteId == routeId))
            .OrderBy(note => note.RouteId).ThenBy(note => note.DisplayOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<RouteNote?> FindByIdAndAgencyIdAsync(int routeNoteId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<RouteNote>().FirstOrDefaultAsync(note => note.Id == routeNoteId && note.AgencyId == agencyId,
            cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByRouteIdAndDisplayOrderAsync(int routeId, int displayOrder,
        int? excludedRouteNoteId = null, CancellationToken cancellationToken = default) =>
        await Context.Set<RouteNote>().AnyAsync(note => note.RouteId == routeId && note.DisplayOrder == displayOrder &&
            (excludedRouteNoteId == null || note.Id != excludedRouteNoteId), cancellationToken);
}
