namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the expected time windows of an agency, optionally filtered by route.
/// </summary>
public record GetAllExpectedTimeWindowsQuery(int AgencyId, int? RouteId);
