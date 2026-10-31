namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a route returned to clients.
/// </summary>
public record RouteResource(
    int Id,
    int AgencyId,
    string Name,
    string Origin,
    string Destination,
    decimal DistanceKm,
    int ElevationGainMeters,
    string Difficulty,
    int EstimatedDurationMinutes,
    string Status);
