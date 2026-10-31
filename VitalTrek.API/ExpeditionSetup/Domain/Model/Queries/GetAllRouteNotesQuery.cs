namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the route notes of an agency, optionally filtered by route.
/// </summary>
public record GetAllRouteNotesQuery(int AgencyId, int? RouteId);
