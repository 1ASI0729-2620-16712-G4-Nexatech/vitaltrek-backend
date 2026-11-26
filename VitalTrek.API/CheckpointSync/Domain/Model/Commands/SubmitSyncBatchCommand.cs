namespace VitalTrek.API.CheckpointSync.Domain.Model.Commands;

/// <summary>
///     Command to receive the readings and checkpoint passages captured offline by a field guide.
///     The agency and the field guide come from the authenticated user, never from the request body.
/// </summary>
/// <param name="AgencyId">The agency of the authenticated field guide.</param>
/// <param name="FieldGuideId">The authenticated field guide.</param>
/// <param name="ClientBatchId">Stable device-generated batch identity, reused on every retry.</param>
/// <param name="SessionId">The tracking session the batch belongs to.</param>
/// <param name="CheckpointId">The checkpoint where the batch was synchronized.</param>
/// <param name="Readings">The telemetry readings captured offline.</param>
/// <param name="Passages">The checkpoint passages captured offline.</param>
public record SubmitSyncBatchCommand(
    int AgencyId,
    int FieldGuideId,
    Guid ClientBatchId,
    int SessionId,
    int CheckpointId,
    IReadOnlyList<SyncBatchReadingEntry> Readings,
    IReadOnlyList<SyncBatchPassageEntry> Passages);

/// <summary>
///     A telemetry reading inside a sync batch. The participant is resolved by the server from the wearable assignment.
/// </summary>
public record SyncBatchReadingEntry(
    Guid ClientRecordId,
    int WearableDeviceId,
    DateTimeOffset RecordedAt,
    double Latitude,
    double Longitude,
    double Altitude,
    int HeartRate,
    int Battery,
    double DeviationMeters);

/// <summary>
///     A checkpoint passage inside a sync batch.
/// </summary>
public record SyncBatchPassageEntry(Guid ClientRecordId, int CheckpointId, DateTimeOffset PassedAt);
