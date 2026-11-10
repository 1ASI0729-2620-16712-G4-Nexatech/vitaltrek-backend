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
}
