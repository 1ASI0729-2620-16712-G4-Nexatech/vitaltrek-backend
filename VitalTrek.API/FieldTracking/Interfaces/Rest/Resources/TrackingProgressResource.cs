namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

public sealed record TrackingProgressResource(int SessionId, int ExpeditionGroupId, int RouteId, string Status,
    DateTimeOffset StartedAt, DateTimeOffset? EndedAt, int TotalCheckpoints, int ConfirmedCheckpointsCount,
    int EstimatedProgressPercentage, ProgressCheckpointResource? LastConfirmedCheckpoint,
    ProgressCheckpointResource? NextCheckpoint, IReadOnlyList<ProgressCheckpointResource> ConfirmedCheckpoints,
    IReadOnlyList<ParticipantTrackingProgressResource> Participants, DateTimeOffset? SynchronizedAt);

public sealed record ProgressCheckpointResource(int CheckpointId, int SequenceOrder, string Location,
    DateTimeOffset? PassedAt, DateTimeOffset? SynchronizedAt);

public sealed record ParticipantTrackingProgressResource(int MemberId, int WearableDeviceId, Guid ClientRecordId,
    double Latitude, double Longitude, double Altitude, int? HeartRate, int? Battery, double? DeviationMeters,
    bool IsDeviationSimulated, DateTimeOffset RecordedAt, DateTimeOffset SynchronizedAt);
