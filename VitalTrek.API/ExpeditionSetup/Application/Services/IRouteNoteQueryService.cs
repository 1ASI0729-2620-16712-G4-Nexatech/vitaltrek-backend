using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;


namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application query service interface for route note read operations.
/// </summary>
public interface IRouteNoteQueryService
{
    /// <summary>Retrieves the route notes of an agency, optionally filtered by route.</summary>
    Task<IEnumerable<RouteNote>> Handle(GetAllRouteNotesQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a route note that belongs to an agency.</summary>
    Task<RouteNote?> Handle(GetRouteNoteByIdQuery query, CancellationToken cancellationToken);
}
