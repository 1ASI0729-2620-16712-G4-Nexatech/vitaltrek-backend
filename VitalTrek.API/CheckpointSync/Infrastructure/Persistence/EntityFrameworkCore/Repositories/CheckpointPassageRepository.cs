using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.CheckpointSync.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="CheckpointPassage"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class CheckpointPassageRepository(AppDbContext context)
    : BaseRepository<CheckpointPassage>(context), ICheckpointPassageRepository
{
    /// <inheritdoc />
    public async Task<CheckpointPassage?> FindByIdAndAgencyIdAsync(int passageId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<CheckpointPassage>().FirstOrDefaultAsync(
            passage => passage.Id == passageId && passage.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<CheckpointPassage>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<CheckpointPassage>()
            .Where(passage => passage.SessionId == sessionId && passage.AgencyId == agencyId)
            .OrderBy(passage => passage.PassedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<CheckpointPassage>> FindByCheckpointIdAndAgencyIdAsync(int checkpointId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<CheckpointPassage>()
            .Where(passage => passage.CheckpointId == checkpointId && passage.AgencyId == agencyId)
            .OrderBy(passage => passage.PassedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<CheckpointPassage?> FindBySessionIdAndCheckpointIdAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<CheckpointPassage>().FirstOrDefaultAsync(
            passage => passage.SessionId == sessionId && passage.CheckpointId == checkpointId, cancellationToken);

    /// <inheritdoc />
    public async Task<DateTimeOffset?> FindLastPassedAtBySessionIdAndCheckpointIdAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<CheckpointPassage>()
            .Where(passage => passage.SessionId == sessionId && passage.CheckpointId == checkpointId)
            .Select(passage => (DateTimeOffset?)passage.PassedAt).MaxAsync(cancellationToken);
}
