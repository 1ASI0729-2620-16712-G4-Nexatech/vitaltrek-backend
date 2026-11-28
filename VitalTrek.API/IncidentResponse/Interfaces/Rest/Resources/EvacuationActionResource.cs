namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a persisted evacuation action. RecordedAt is the capture in the field and SynchronizedAt the
///     reception by the platform.
/// </summary>
public record EvacuationActionResource(
    int Id,
    Guid ClientActionId,
    int IncidentId,
    int ExpeditionGroupId,
    int RecordedByUserId,
    string Description,
    string? Location,
    string Status,
    DateTimeOffset RecordedAt,
    DateTimeOffset SynchronizedAt);
