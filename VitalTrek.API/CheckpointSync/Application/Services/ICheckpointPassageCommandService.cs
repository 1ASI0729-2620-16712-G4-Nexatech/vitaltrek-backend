using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.CheckpointSync.Application.Services;

/// <summary>
///     Application command service interface for checkpoint passage write operations.
/// </summary>
public interface ICheckpointPassageCommandService
{
    /// <summary>
    ///     Records a passage of the authenticated field guide in a session of a group the guide is assigned to.
    ///     The operation is idempotent per session and checkpoint: when a passage already exists it is returned
    ///     unchanged and <c>WasCreated</c> is false.
    /// </summary>
    Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> Handle(
        RecordCheckpointPassageCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>
    ///     Validates and stages a passage inside the caller's unit of work. Never saves. Checks the route, that the
    ///     passage time lies within the session and the synchronization, and that the previous route checkpoint was
    ///     passed before. When the checkpoint already has a passage, persisted or staged, the first one is returned
    ///     unchanged and <c>WasCreated</c> is false.
    /// </summary>
    /// <param name="command">The passage to stage.</param>
    /// <param name="scope">The session scope already resolved for the actor.</param>
    /// <param name="syncedAt">The reception moment of the passage.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> StagePassage(
        RecordCheckpointPassageCommand command, SyncSessionScope scope, DateTimeOffset syncedAt,
        CancellationToken cancellationToken);
}
