namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the safety protocols of an agency, optionally filtered by route.
/// </summary>
public record GetAllSafetyProtocolsQuery(int AgencyId, int? RouteId);
