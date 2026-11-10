using Microsoft.EntityFrameworkCore;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of <see cref="ITrackingSessionRepository"/>.
/// </summary>
public class TrackingSessionRepository(AppDbContext context)
    : BaseRepository<TrackingSession>(context), ITrackingSessionRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<TrackingSession>> FindAllByAgencyIdAsync(int agencyId, CancellationToken ct = default) =>
        await Context.Set<TrackingSession>()
            .Where(s => s.AgencyId == agencyId)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<TrackingSession?> FindByIdAndAgencyIdAsync(int sessionId, int agencyId, CancellationToken ct = default) =>
        await Context.Set<TrackingSession>()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.AgencyId == agencyId, ct);

    /// <inheritdoc />
    public async Task<TrackingSession?> FindActiveByGroupIdAsync(int expeditionGroupId, CancellationToken ct = default) =>
        await Context.Set<TrackingSession>()
            .FirstOrDefaultAsync(s => s.ExpeditionGroupId == expeditionGroupId && s.Status == SessionStatus.Active, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsActiveByGroupIdAsync(int expeditionGroupId, CancellationToken ct = default) =>
        await Context.Set<TrackingSession>()
            .AnyAsync(s => s.ExpeditionGroupId == expeditionGroupId && s.Status == SessionStatus.Active, ct);
}
