namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to set the expected time window of a route segment between two consecutive checkpoints.
/// </summary>
public record CreateExpectedTimeWindowCommand(
    int AgencyId,
    int RouteId,
    int FromCheckpointId,
    int ToCheckpointId,
    int MinimumMinutes,
    int MaximumMinutes);
