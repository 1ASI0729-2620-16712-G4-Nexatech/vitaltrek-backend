using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.CheckpointSync.Application.Services;

/// <summary>
///     Application command service interface for sync batch reception.
/// </summary>
public interface ISyncBatchCommandService
{
    /// <summary>
    ///     Receives a sync batch. The batch, its accepted readings and its valid passages are saved in one transaction.
    ///     Repeating the same batch returns the persisted result unchanged.
    /// </summary>
    Task<Result<SyncBatch, ESubmitSyncBatchError>> Handle(SubmitSyncBatchCommand command, AuthenticatedActor actor,
        CancellationToken cancellationToken);
}
