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

    /// <summary>
    ///     Validates and stages a passage inside the caller's unit of work. Never saves. The caller has already
    ///     validated the session scope and the route. When the session and checkpoint already have a passage, persisted
    ///     or staged, the first one is returned unchanged and <c>WasCreated</c> is false.
    /// </summary>
    /// <param name="command">The passage to stage.</param>
    /// <param name="syncedAt">The reception moment of the batch that carries the passage.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> StagePassage(
        RecordCheckpointPassageCommand command, DateTimeOffset syncedAt, CancellationToken cancellationToken);
}
