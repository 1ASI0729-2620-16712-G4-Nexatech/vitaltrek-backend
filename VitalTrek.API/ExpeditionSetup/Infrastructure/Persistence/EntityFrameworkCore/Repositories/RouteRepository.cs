using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="Route"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class RouteRepository(AppDbContext context) : BaseRepository<Route>(context), IRouteRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Route>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default) =>
        await Context.Set<Route>().Where(route => route.AgencyId == agencyId).OrderBy(route => route.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Route?> FindByIdAndAgencyIdAsync(int routeId, int agencyId, CancellationToken cancellationToken = default) =>
        await Context.Set<Route>().FirstOrDefaultAsync(route => route.Id == routeId && route.AgencyId == agencyId,
            cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByAgencyIdAndNameAsync(int agencyId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        return await Context.Set<Route>().AnyAsync(
            route => route.AgencyId == agencyId && route.Name.ToLower() == normalizedName, cancellationToken);
    }
}
