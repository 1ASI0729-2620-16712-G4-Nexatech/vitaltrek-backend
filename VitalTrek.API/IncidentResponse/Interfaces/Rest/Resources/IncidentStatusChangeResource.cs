namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing one recorded transition of an incident.
/// </summary>
public record IncidentStatusChangeResource(
    string? FromStatus,
    string ToStatus,
    int ActorUserId,
    string? Note,
    DateTimeOffset OccurredAt);
