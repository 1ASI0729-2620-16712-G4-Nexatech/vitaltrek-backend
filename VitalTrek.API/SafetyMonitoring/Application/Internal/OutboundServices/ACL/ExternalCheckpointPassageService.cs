using VitalTrek.API.CheckpointSync.Interfaces.ACL;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that reads checkpoint passages through the Checkpoint Sync facade.
/// </summary>
/// <param name="checkpointSyncContextFacade">The Checkpoint Sync facade.</param>
public class ExternalCheckpointPassageService(ICheckpointSyncContextFacade checkpointSyncContextFacade)
{
    /// <summary>Returns the latest time a checkpoint was passed within a session, or null if it was not passed.</summary>
    public async Task<DateTimeOffset?> FindLastPassageTime(int sessionId, int checkpointId,
        CancellationToken cancellationToken) =>
        await checkpointSyncContextFacade.GetLastPassageTimeAtCheckpointBySessionAsync(sessionId, checkpointId,
            cancellationToken);
}
