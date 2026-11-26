namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;

/// <summary>
///     Persisted result of a sync batch. Repeating the same batch returns this same result.
/// </summary>
public record SyncBatchResource(
    int BatchId,
    Guid ClientBatchId,
    int SessionId,
    int CheckpointId,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<Guid> AcceptedRecordIds,
    IReadOnlyList<Guid> DuplicateRecordIds,
    IReadOnlyList<RejectedSyncRecordResource> RejectedRecords,
    string EvaluationStatus);

/// <summary>
///     A record of the batch that was not stored, with its stable rejection code.
/// </summary>
public record RejectedSyncRecordResource(Guid ClientRecordId, string Code);
