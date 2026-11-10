using Microsoft.EntityFrameworkCore;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of <see cref="ITelemetryRecordRepository"/>.
/// </summary>
public class TelemetryRecordRepository(AppDbContext context)
    : BaseRepository<TelemetryRecord>(context), ITelemetryRecordRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<TelemetryRecord>> FindAllBySessionIdAsync(int sessionId, CancellationToken ct = default) =>
        await Context.Set<TelemetryRecord>()
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.RecordedAt)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<TelemetryRecord?> FindLatestBySessionIdAndWearableIdAsync(
        int sessionId, int wearableDeviceId, CancellationToken ct = default) =>
        await Context.Set<TelemetryRecord>()
            .Where(r => r.SessionId == sessionId && r.WearableDeviceId == wearableDeviceId)
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<TelemetryRecord> records, CancellationToken ct = default) =>
        await Context.Set<TelemetryRecord>().AddRangeAsync(records, ct);
}
