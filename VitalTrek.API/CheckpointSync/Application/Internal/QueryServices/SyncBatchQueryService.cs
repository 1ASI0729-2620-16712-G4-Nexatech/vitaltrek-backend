using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Queries;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.CheckpointSync.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ISyncBatchQueryService"/>.
/// </summary>
/// <param name="syncBatchRepository">The sync batch repository.</param>
/// <param name="externalFieldTrackingService">The outbound service that resolves the actor's session scope.</param>
public class SyncBatchQueryService(
    ISyncBatchRepository syncBatchRepository,
    ExternalFieldTrackingService externalFieldTrackingService) : ISyncBatchQueryService
{
    /// <inheritdoc />
    public async Task<SyncBatch?> Handle(GetSyncBatchByIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (query.AgencyId != actor.AgencyId) return null;
        var batch = await syncBatchRepository.FindByIdAndAgencyIdAsync(query.BatchId, query.AgencyId, cancellationToken);
        if (batch is null) return null;
        return await externalFieldTrackingService.ResolveSessionScope(actor, batch.SessionId, cancellationToken) is null
            ? null
            : batch;
    }
}
