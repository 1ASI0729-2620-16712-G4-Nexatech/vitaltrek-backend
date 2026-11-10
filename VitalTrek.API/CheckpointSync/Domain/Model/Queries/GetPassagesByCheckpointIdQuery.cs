namespace VitalTrek.API.CheckpointSync.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the passages registered at a checkpoint.
/// </summary>
public record GetPassagesByCheckpointIdQuery(int AgencyId, int CheckpointId);
