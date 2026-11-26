namespace VitalTrek.API.FieldTracking.Application.Model;

/// <summary>Persisted progress and the latest capture of each participant visible to the actor.</summary>
public sealed record TrackingProgress(int SessionId, int ExpeditionGroupId, int RouteId, string Status,
    DateTimeOffset StartedAt, DateTimeOffset? EndedAt, int TotalCheckpoints, int ConfirmedCheckpointsCount,
    int EstimatedProgressPercentage, ProgressCheckpoint? LastConfirmedCheckpoint, ProgressCheckpoint? NextCheckpoint,
    IReadOnlyList<ProgressCheckpoint> ConfirmedCheckpoints, IReadOnlyList<ParticipantTrackingProgress> Participants,
    DateTimeOffset? SynchronizedAt);

public sealed record ProgressCheckpoint(int CheckpointId, int SequenceOrder, string Location,
    DateTimeOffset? PassedAt = null, DateTimeOffset? SynchronizedAt = null);

public sealed record ParticipantTrackingProgress(int MemberId, int WearableDeviceId, Guid ClientRecordId,
    double Latitude, double Longitude, double Altitude, int? HeartRate, int? Battery, double? DeviationMeters,
    bool IsDeviationSimulated, DateTimeOffset RecordedAt, DateTimeOffset SynchronizedAt);
