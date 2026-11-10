namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a tracking session returned to clients.
/// </summary>
public record TrackingSessionResource(
    int Id,
    int AgencyId,
    int ExpeditionGroupId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Status);
