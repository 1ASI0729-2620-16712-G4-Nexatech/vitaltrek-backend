using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register a route of an agency.
/// </summary>
public record CreateRouteCommand(
    int AgencyId,
    string Name,
    string Origin,
    string Destination,
    decimal DistanceKm,
    int ElevationGainMeters,
    RouteDifficulty Difficulty,
    int EstimatedDurationMinutes);
