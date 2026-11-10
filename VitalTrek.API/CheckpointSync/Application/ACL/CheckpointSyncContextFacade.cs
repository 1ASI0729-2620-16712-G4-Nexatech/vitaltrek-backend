using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;

namespace VitalTrek.API.CheckpointSync.Application.ACL;

/// <summary>
///     Implementation of <see cref="ICheckpointSyncContextFacade"/> backed by the Checkpoint Sync repositories.
/// </summary>
/// <param name="passageRepository">The checkpoint passage repository.</param>
public class CheckpointSyncContextFacade(ICheckpointPassageRepository passageRepository) : ICheckpointSyncContextFacade
{
    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetLastPassageTimeAtCheckpointBySessionAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default) =>
        await passageRepository.FindLastPassedAtBySessionIdAndCheckpointIdAsync(sessionId, checkpointId, cancellationToken);
}
