using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IRouteQueryService"/>.
/// </summary>
/// <param name="routeRepository">The route repository.</param>
public class RouteQueryService(IRouteRepository routeRepository) : IRouteQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<Route>> Handle(GetAllRoutesByAgencyIdQuery query, CancellationToken cancellationToken) =>
        await routeRepository.FindByAgencyIdAsync(query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<Route?> Handle(GetRouteByIdQuery query, CancellationToken cancellationToken) =>
        await routeRepository.FindByIdAndAgencyIdAsync(query.RouteId, query.AgencyId, cancellationToken);
}
