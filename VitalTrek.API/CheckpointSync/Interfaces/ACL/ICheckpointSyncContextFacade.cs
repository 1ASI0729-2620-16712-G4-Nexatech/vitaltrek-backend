namespace VitalTrek.API.CheckpointSync.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes Checkpoint Sync information to other bounded contexts
///     without sharing the Checkpoint Sync domain model.
/// </summary>
public interface ICheckpointSyncContextFacade
{
    /// <summary>
    ///     Returns the latest time a checkpoint was passed within a tracking session, or null if there is no passage.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier.</param>
    /// <param name="checkpointId">The checkpoint identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The latest passage time, or <see langword="null"/> when no passage exists.</returns>
    Task<DateTimeOffset?> GetLastPassageTimeAtCheckpointBySessionAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default);
}
