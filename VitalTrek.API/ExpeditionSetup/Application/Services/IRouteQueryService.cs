using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application query service interface for route read operations.
/// </summary>
public interface IRouteQueryService
{
    /// <summary>Retrieves the routes of an agency.</summary>
    Task<IEnumerable<Route>> Handle(GetAllRoutesByAgencyIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a route that belongs to an agency.</summary>
    Task<Route?> Handle(GetRouteByIdQuery query, CancellationToken cancellationToken);
}
