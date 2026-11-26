using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.CheckpointSync.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="SyncBatch"/> aggregates and their record results.
/// </summary>
public interface ISyncBatchRepository : IBaseRepository<SyncBatch>
{
    /// <summary>Finds a batch of an agency, including its record results.</summary>
    Task<SyncBatch?> FindByIdAndAgencyIdAsync(int batchId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the batch registered with a client identity in a session, including its record results.</summary>
    Task<SyncBatch?> FindBySessionIdAndClientBatchIdAsync(int sessionId, Guid clientBatchId,
        CancellationToken cancellationToken = default);
}
