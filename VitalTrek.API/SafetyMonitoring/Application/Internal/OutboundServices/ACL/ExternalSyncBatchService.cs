using VitalTrek.API.CheckpointSync.Interfaces.ACL;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that reads persisted sync batches from Checkpoint Sync through its anti-corruption layer facade
///     and translates them into the data the safety evaluation needs.
/// </summary>
/// <param name="syncBatchContextFacade">The Checkpoint Sync facade for persisted sync batches.</param>
public class ExternalSyncBatchService(ISyncBatchContextFacade syncBatchContextFacade)
{
    // Evaluation status code published by the ISyncBatchContextFacade contract.
    private const string EvaluatedStatusCode = "evaluated";

    /// <summary>
    ///     Finds a sync batch of an agency with the readings it accepted, or <see langword="null"/> when the batch does
    ///     not exist in that agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="batchId">The sync batch identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    public async Task<SyncBatchToEvaluate?> FindBatchToEvaluate(int agencyId, int batchId,
        CancellationToken cancellationToken)
    {
        if (agencyId <= 0 || batchId <= 0) return null;
        var batch = await syncBatchContextFacade.FindBatchForEvaluationAsync(agencyId, batchId, cancellationToken);
        if (batch is null || batch.AgencyId != agencyId) return null;
        return new SyncBatchToEvaluate(batch.BatchId, batch.AgencyId, batch.SessionId,
            string.Equals(batch.EvaluationStatus, EvaluatedStatusCode, StringComparison.OrdinalIgnoreCase),
            batch.AcceptedReadings
                .Select(reading => new ReadingToEvaluate(reading.ClientRecordId, reading.MemberId, reading.RecordedAt,
                    reading.HeartRate, reading.DeviationMeters))
                .ToArray());
    }

    /// <summary>
    ///     Stages the evaluated status of a batch without saving it, so it is committed together with the alerts.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="batchId">The sync batch identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the batch was found in the agency.</returns>
    public Task<bool> StageEvaluationCompleted(int agencyId, int batchId, CancellationToken cancellationToken) =>
        syncBatchContextFacade.StageEvaluationCompletedAsync(agencyId, batchId, cancellationToken);
}

/// <summary>A persisted sync batch as seen by the safety evaluation.</summary>
/// <param name="BatchId">The sync batch identifier.</param>
/// <param name="AgencyId">The agency that owns the batch.</param>
/// <param name="SessionId">The tracking session of the batch.</param>
/// <param name="IsEvaluated">Whether the readings of the batch were already evaluated.</param>
/// <param name="Readings">The readings accepted by this batch.</param>
public sealed record SyncBatchToEvaluate(int BatchId, int AgencyId, int SessionId, bool IsEvaluated,
    IReadOnlyList<ReadingToEvaluate> Readings);

/// <summary>An accepted reading as seen by the safety evaluation.</summary>
/// <param name="ClientRecordId">The stable client identity of the reading.</param>
/// <param name="MemberId">The participant resolved by the server from the wearable assignment, if any.</param>
/// <param name="RecordedAt">The original capture time of the reading.</param>
/// <param name="HeartRate">The heart rate, in beats per minute, if measured.</param>
/// <param name="DeviationMeters">The simulated route deviation, in meters, if reported.</param>
public sealed record ReadingToEvaluate(Guid ClientRecordId, int? MemberId, DateTimeOffset RecordedAt, int? HeartRate,
    double? DeviationMeters);
