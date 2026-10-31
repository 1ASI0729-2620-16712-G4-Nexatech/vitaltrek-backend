using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a route. Difficulty accepts Beginner, Intermediate, Advanced or Expert.
/// </summary>
public record CreateRouteResource(
    [Required(ErrorMessage = "RequiredRouteName")] string Name,
    [Required(ErrorMessage = "RequiredOrigin")] string Origin,
    [Required(ErrorMessage = "RequiredDestination")] string Destination,
    [Range(0.01, 10000, ErrorMessage = "InvalidDistance")] decimal DistanceKm,
    [Range(0, 10000, ErrorMessage = "InvalidElevationGain")] int ElevationGainMeters,
    [Required(ErrorMessage = "RequiredDifficulty")] string Difficulty,
    [Range(1, 100000, ErrorMessage = "InvalidEstimatedDuration")] int EstimatedDurationMinutes);
