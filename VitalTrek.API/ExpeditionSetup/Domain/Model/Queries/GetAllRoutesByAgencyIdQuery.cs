namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the routes of an agency.
/// </summary>
public record GetAllRoutesByAgencyIdQuery(int AgencyId);
