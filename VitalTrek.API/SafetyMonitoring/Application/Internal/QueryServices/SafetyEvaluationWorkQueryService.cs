using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ISafetyEvaluationWorkQueryService"/> through the
///     outbound services of Checkpoint Sync and Field Tracking.
/// </summary>
/// <param name="externalSyncBatchService">The outbound service that reads the persisted sync batches.</param>
/// <param name="externalTrackingSessionService">The outbound service that reads the tracking sessions.</param>
public class SafetyEvaluationWorkQueryService(
    ExternalSyncBatchService externalSyncBatchService,
    ExternalTrackingSessionService externalTrackingSessionService) : ISafetyEvaluationWorkQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingSyncBatch>> Handle(GetPendingSyncBatchEvaluationsQuery query,
        CancellationToken cancellationToken) =>
        query.MaxCount <= 0 ? [] : await externalSyncBatchService.FindPendingBatches(query.MaxCount, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveSession>> Handle(GetActiveTrackingSessionsQuery query,
        CancellationToken cancellationToken) =>
        query.MaxCount <= 0 ? [] : await externalTrackingSessionService.FindActiveSessions(query.MaxCount, cancellationToken);
}
