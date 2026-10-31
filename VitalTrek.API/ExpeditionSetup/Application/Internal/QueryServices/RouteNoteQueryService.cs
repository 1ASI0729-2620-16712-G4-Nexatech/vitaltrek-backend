using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IRouteNoteQueryService"/>.
/// </summary>
/// <param name="routeNoteRepository">The repository.</param>
public class RouteNoteQueryService(IRouteNoteRepository routeNoteRepository) : IRouteNoteQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RouteNote>> Handle(GetAllRouteNotesQuery query, CancellationToken cancellationToken) =>
        await routeNoteRepository.FindByAgencyIdAsync(query.AgencyId, query.RouteId, cancellationToken);

    /// <inheritdoc />
    public async Task<RouteNote?> Handle(GetRouteNoteByIdQuery query, CancellationToken cancellationToken) =>
        await routeNoteRepository.FindByIdAndAgencyIdAsync(query.RouteNoteId, query.AgencyId, cancellationToken);
}
