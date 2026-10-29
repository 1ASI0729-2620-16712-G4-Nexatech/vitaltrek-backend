namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the checkpoints of an agency, optionally filtered by route.
/// </summary>
public record GetAllCheckpointsQuery(int AgencyId, int? RouteId);
