using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.CheckpointSync.Application.Services;

/// <summary>
///     Application command service interface for checkpoint passage write operations.
/// </summary>
public interface ICheckpointPassageCommandService
{
    /// <summary>
    ///     Records a checkpoint passage. The operation is idempotent per session and checkpoint: when a passage
    ///     already exists it is returned unchanged and <c>WasCreated</c> is false.
    /// </summary>
    Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> Handle(
        RecordCheckpointPassageCommand command, CancellationToken cancellationToken);
}
