using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;

/// <summary>
///     Resource to record the passage of the authenticated field guide through a checkpoint.
/// </summary>
public record RecordCheckpointPassageResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredSessionId")] int SessionId,
    [Range(1, int.MaxValue, ErrorMessage = "RequiredCheckpointId")] int CheckpointId,
    [Required(ErrorMessage = "RequiredPassedAt")] DateTimeOffset PassedAt);
