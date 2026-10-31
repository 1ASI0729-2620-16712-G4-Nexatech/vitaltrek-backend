namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a route of an agency.
/// </summary>
public record GetRouteByIdQuery(int AgencyId, int RouteId);
