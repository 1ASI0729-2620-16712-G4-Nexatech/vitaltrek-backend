namespace VitalTrek.API.CheckpointSync.Domain.Model.Commands;

/// <summary>
///     Command to record the passage of a field guide through a physical checkpoint.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="FieldGuideId">The field guide who passed the checkpoint.</param>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="CheckpointId">The checkpoint identifier.</param>
/// <param name="PassedAt">The moment the checkpoint was passed.</param>
public record RecordCheckpointPassageCommand(int AgencyId, int FieldGuideId, int SessionId, int CheckpointId,
    DateTimeOffset PassedAt);
