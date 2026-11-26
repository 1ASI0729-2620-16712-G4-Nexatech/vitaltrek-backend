using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP response resources from Checkpoint Sync aggregates.
/// </summary>
public static class CheckpointSyncResourceAssembler
{
    /// <summary>Transforms a <see cref="CheckpointPassage"/> into a <see cref="CheckpointPassageResource"/>.</summary>
    public static CheckpointPassageResource ToResourceFromEntity(CheckpointPassage entity) =>
        new(entity.Id, entity.SessionId, entity.CheckpointId, entity.FieldGuideId, entity.AgencyId, entity.PassedAt,
            entity.SyncedAt, entity.Status.ToCode());

    /// <summary>Transforms a <see cref="SyncBatch"/> into a <see cref="SyncBatchResource"/>.</summary>
    public static SyncBatchResource ToResourceFromEntity(SyncBatch entity) =>
        new(entity.Id, entity.ClientBatchId, entity.SessionId, entity.CheckpointId, entity.ReceivedAt,
            entity.RecordIdsWith(SyncRecordOutcome.Accepted), entity.RecordIdsWith(SyncRecordOutcome.Duplicate),
            entity.RejectedRecords().Select(record => new RejectedSyncRecordResource(record.ClientRecordId,
                record.RejectionCode!)).ToArray(),
            entity.EvaluationStatus.ToCode());
}
