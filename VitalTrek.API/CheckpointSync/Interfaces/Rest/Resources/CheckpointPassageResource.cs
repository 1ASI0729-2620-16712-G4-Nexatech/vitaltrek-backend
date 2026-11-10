namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a checkpoint passage returned to clients.
/// </summary>
public record CheckpointPassageResource(
    int Id,
    int SessionId,
    int CheckpointId,
    int FieldGuideId,
    int AgencyId,
    DateTimeOffset PassedAt,
    DateTimeOffset SyncedAt,
    string Status);
