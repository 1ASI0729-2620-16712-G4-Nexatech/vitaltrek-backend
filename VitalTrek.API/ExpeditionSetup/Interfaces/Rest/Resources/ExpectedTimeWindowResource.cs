namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an expected time window returned to clients.
/// </summary>
public record ExpectedTimeWindowResource(
    int Id,
    int AgencyId,
    int RouteId,
    int FromCheckpointId,
    int ToCheckpointId,
    int MinimumMinutes,
    int MaximumMinutes);
