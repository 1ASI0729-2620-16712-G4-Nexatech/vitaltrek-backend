using VitalTrek.API.Shared.Domain.Repositories;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="Route"/> aggregates.
/// </summary>
public interface IRouteRepository : IBaseRepository<Route>
{
    /// <summary>Finds the routes of an agency.</summary>
    Task<IEnumerable<Route>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds a route that belongs to an agency.</summary>
    Task<Route?> FindByIdAndAgencyIdAsync(int routeId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether the agency already has a route with the given name (case-insensitive).</summary>
    Task<bool> ExistsByAgencyIdAndNameAsync(int agencyId, string name, CancellationToken cancellationToken = default);
}
