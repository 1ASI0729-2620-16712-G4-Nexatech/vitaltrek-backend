using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to set the expected time window of a segment between two consecutive checkpoints.
/// </summary>
public record CreateExpectedTimeWindowResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredRouteId")] int RouteId,
    [Range(1, int.MaxValue, ErrorMessage = "RequiredCheckpointId")] int FromCheckpointId,
    [Range(1, int.MaxValue, ErrorMessage = "RequiredCheckpointId")] int ToCheckpointId,
    [Range(0, 100000, ErrorMessage = "InvalidMinutes")] int MinimumMinutes,
    [Range(1, 100000, ErrorMessage = "InvalidMinutes")] int MaximumMinutes);
