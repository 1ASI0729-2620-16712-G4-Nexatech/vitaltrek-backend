namespace VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;

public sealed record ConfirmedCheckpointPassage(int CheckpointId, int FieldGuideId,
    DateTimeOffset PassedAt, DateTimeOffset SynchronizedAt);
