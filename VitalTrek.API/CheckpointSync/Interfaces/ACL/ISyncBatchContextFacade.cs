using VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;

namespace VitalTrek.API.CheckpointSync.Interfaces.ACL;

/// <summary>
///     Internal anti-corruption layer facade that hands persisted sync batches to Safety Monitoring (D01 and D02).
///     It is not exposed over REST; every lookup is scoped to the agency that owns the batch.
/// </summary>
public interface ISyncBatchContextFacade
{
    /// <summary>
    ///     Returns the accepted readings, counts and evaluation status of a batch of the agency, or null when the batch
    ///     does not exist in that agency.
    /// </summary>
    Task<SyncBatchEvaluationInput?> FindBatchForEvaluationAsync(int agencyId, int batchId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the oldest batches still pending evaluation (at most 100), to recover them after a failure.</summary>
    Task<IReadOnlyList<PendingSyncBatchEvaluation>> FindPendingEvaluationsAsync(int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stages the evaluated status of a batch of the agency. Does not save: the caller saves it in the same unit of
    ///     work as its alerts, so a failed evaluation stays pending for a retry. Returns false when the batch is not found.
    /// </summary>
    Task<bool> StageEvaluationCompletedAsync(int agencyId, int batchId, CancellationToken cancellationToken = default);
}
