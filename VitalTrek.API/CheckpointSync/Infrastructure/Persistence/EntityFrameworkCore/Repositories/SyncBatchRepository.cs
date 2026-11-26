using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Entities;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.CheckpointSync.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="SyncBatch"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class SyncBatchRepository(AppDbContext context) : BaseRepository<SyncBatch>(context), ISyncBatchRepository
{
    /// <inheritdoc />
    public async Task<SyncBatch?> FindByIdAndAgencyIdAsync(int batchId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<SyncBatch>().Include(batch => batch.Records)
            .FirstOrDefaultAsync(batch => batch.Id == batchId && batch.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<SyncBatch?> FindBySessionIdAndClientBatchIdAsync(int sessionId, Guid clientBatchId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<SyncBatch>().Include(batch => batch.Records)
            .FirstOrDefaultAsync(batch => batch.SessionId == sessionId && batch.ClientBatchId == clientBatchId,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncBatchRecord>> FindNonRejectedRecordsAsync(int sessionId,
        IReadOnlyCollection<Guid> clientRecordIds, CancellationToken cancellationToken = default)
    {
        if (clientRecordIds.Count == 0) return [];
        return await Context.Set<SyncBatch>().AsNoTracking().Where(batch => batch.SessionId == sessionId)
            .SelectMany(batch => batch.Records)
            .Where(record => clientRecordIds.Contains(record.ClientRecordId) &&
                             record.Outcome != SyncRecordOutcome.Rejected)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncBatch>> FindPendingEvaluationAsync(int maxCount,
        CancellationToken cancellationToken = default) =>
        await Context.Set<SyncBatch>().AsNoTracking()
            .Where(batch => batch.EvaluationStatus == SyncBatchEvaluationStatus.Pending)
            .OrderBy(batch => batch.ReceivedAt).ThenBy(batch => batch.Id)
            .Take(Math.Clamp(maxCount, 1, 100)).ToListAsync(cancellationToken);
}
