namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a route note of an agency.
/// </summary>
public record GetRouteNoteByIdQuery(int AgencyId, int RouteNoteId);
