using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Interfaces.ACL;

namespace VitalTrek.API.CheckpointSync.Application.ACL;

/// <summary>
///     Implementation of <see cref="ISyncBatchContextFacade"/>. Readings are read through the Field Tracking facade,
///     which owns the telemetry model.
/// </summary>
/// <param name="syncBatchRepository">The sync batch repository.</param>
/// <param name="fieldTrackingContextFacade">The Field Tracking facade.</param>
public class SyncBatchContextFacade(ISyncBatchRepository syncBatchRepository,
    IFieldTrackingContextFacade fieldTrackingContextFacade) : ISyncBatchContextFacade
{
    /// <inheritdoc />
    public async Task<SyncBatchEvaluationInput?> FindBatchForEvaluationAsync(int agencyId, int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await syncBatchRepository.FindByIdAndAgencyIdAsync(batchId, agencyId, cancellationToken);
        if (batch is null) return null;
        var accepted = batch.Records.Where(record => record.Outcome == SyncRecordOutcome.Accepted).ToArray();
        var readingIds = accepted.Where(record => record.Kind == SyncRecordKind.Reading)
            .Select(record => record.ClientRecordId).ToArray();
        var readings = await fieldTrackingContextFacade.FindReadingsAsync(batch.AgencyId, batch.SessionId, readingIds,
            cancellationToken);
        return new SyncBatchEvaluationInput(batch.Id, batch.AgencyId, batch.SessionId, batch.CheckpointId,
            batch.ReceivedAt, readings,
            accepted.Where(record => record.Kind == SyncRecordKind.Passage).Select(record => record.ClientRecordId)
                .Order().ToArray(),
            accepted.Length, batch.RecordIdsWith(SyncRecordOutcome.Duplicate).Count, batch.RejectedRecords().Count,
            batch.EvaluationStatus.ToCode());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingSyncBatchEvaluation>> FindPendingEvaluationsAsync(int maxCount,
        CancellationToken cancellationToken = default) =>
        (await syncBatchRepository.FindPendingEvaluationAsync(maxCount, cancellationToken))
        .Select(batch => new PendingSyncBatchEvaluation(batch.Id, batch.AgencyId, batch.SessionId, batch.ReceivedAt))
        .ToArray();

    /// <inheritdoc />
    public async Task<bool> StageEvaluationCompletedAsync(int agencyId, int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await syncBatchRepository.FindByIdAndAgencyIdAsync(batchId, agencyId, cancellationToken);
        if (batch is null) return false;
        batch.MarkEvaluated();
        return true;
    }
}
