using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;

namespace VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;

/// <summary>
///     What Safety Monitoring needs to evaluate one persisted sync batch: the readings it accepted, with their original
///     capture time, its counts and its evaluation status. Duplicates were evaluated with the batch that accepted them.
/// </summary>
/// <param name="BatchId">The batch identifier.</param>
/// <param name="AgencyId">The agency that owns the batch.</param>
/// <param name="SessionId">The tracking session.</param>
/// <param name="CheckpointId">The checkpoint where the batch was synchronized.</param>
/// <param name="ReceivedAt">The reception moment.</param>
/// <param name="AcceptedReadings">The readings stored by this batch.</param>
/// <param name="AcceptedPassageIds">The client identities of the passages stored by this batch.</param>
/// <param name="AcceptedCount">Readings and passages stored by this batch.</param>
/// <param name="DuplicateCount">Records that were already stored.</param>
/// <param name="RejectedCount">Records rejected with a code.</param>
/// <param name="EvaluationStatus">The evaluation status code: pending or evaluated.</param>
public sealed record SyncBatchEvaluationInput(int BatchId, int AgencyId, int SessionId, int CheckpointId,
    DateTimeOffset ReceivedAt, IReadOnlyList<TelemetryReadingSnapshot> AcceptedReadings,
    IReadOnlyList<Guid> AcceptedPassageIds, int AcceptedCount, int DuplicateCount, int RejectedCount,
    string EvaluationStatus);

/// <summary>A batch whose evaluation is still pending, for recovery after a failure or a restart.</summary>
public sealed record PendingSyncBatchEvaluation(int BatchId, int AgencyId, int SessionId, DateTimeOffset ReceivedAt);
