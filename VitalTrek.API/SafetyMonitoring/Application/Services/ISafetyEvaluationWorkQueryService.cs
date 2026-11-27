using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

namespace VitalTrek.API.SafetyMonitoring.Application.Services;

/// <summary>
///     Application query service that lists the pending safety evaluation work of every agency, so the periodic
///     evaluation recovers batches left pending by a failure or a restart and keeps the delays of active sessions current.
/// </summary>
public interface ISafetyEvaluationWorkQueryService
{
    /// <summary>Returns the persisted sync batches whose evaluation is still pending, oldest first.</summary>
    Task<IReadOnlyList<PendingSyncBatch>> Handle(GetPendingSyncBatchEvaluationsQuery query,
        CancellationToken cancellationToken);

    /// <summary>Returns the active tracking sessions, oldest first.</summary>
    Task<IReadOnlyList<ActiveSession>> Handle(GetActiveTrackingSessionsQuery query, CancellationToken cancellationToken);
}
