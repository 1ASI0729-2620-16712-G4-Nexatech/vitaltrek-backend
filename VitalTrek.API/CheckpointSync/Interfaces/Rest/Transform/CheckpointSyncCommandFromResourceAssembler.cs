using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Transform;

/// <summary>
///     Assembles Checkpoint Sync domain commands from REST resources. The agency and the field guide always come
///     from the authenticated user, never from the request body.
/// </summary>
public static class CheckpointSyncCommandFromResourceAssembler
{
    /// <summary>
    ///     Transforms a <see cref="RecordCheckpointPassageResource"/> into a <see cref="RecordCheckpointPassageCommand"/>.
    /// </summary>
    public static RecordCheckpointPassageCommand ToCommandFromResource(RecordCheckpointPassageResource resource,
        int agencyId, int fieldGuideId) =>
        new(agencyId, fieldGuideId, resource.SessionId, resource.CheckpointId, resource.PassedAt);

    /// <summary>
    ///     Transforms a <see cref="SubmitSyncBatchResource"/> into a <see cref="SubmitSyncBatchCommand"/>.
    /// </summary>
    public static SubmitSyncBatchCommand ToCommandFromResource(SubmitSyncBatchResource resource, int agencyId,
        int fieldGuideId) =>
        new(agencyId, fieldGuideId, resource.ClientBatchId, resource.SessionId, resource.CheckpointId,
            (resource.Readings ?? []).Select(reading => new SyncBatchReadingEntry(reading.ClientRecordId,
                reading.WearableDeviceId, reading.RecordedAt, reading.Latitude, reading.Longitude, reading.Altitude,
                reading.HeartRate, reading.Battery, reading.DeviationMeters)).ToArray(),
            (resource.Passages ?? []).Select(passage => new SyncBatchPassageEntry(passage.ClientRecordId,
                passage.CheckpointId, passage.PassedAt)).ToArray());
}
