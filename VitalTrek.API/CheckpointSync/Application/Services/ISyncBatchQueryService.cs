using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Queries;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.CheckpointSync.Application.Services;

/// <summary>
///     Application query service interface for sync batch read operations.
/// </summary>
public interface ISyncBatchQueryService
{
    /// <summary>Retrieves a batch only when its session is within the actor's agency and group scope.</summary>
    Task<SyncBatch?> Handle(GetSyncBatchByIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);
}
