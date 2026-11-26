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
    public async Task<IReadOnlyList<TelemetryRecord>> FindLatestForMembersAsync(int sessionId,
        IReadOnlyCollection<int> memberIds, CancellationToken ct = default)
    {
        if (memberIds.Count == 0) return [];
        var records = Context.Set<TelemetryRecord>().AsNoTracking();
        return await records.Where(record => record.SessionId == sessionId && record.MemberId.HasValue &&
            memberIds.Contains(record.MemberId.Value) && !records.Any(newer => newer.SessionId == sessionId &&
                newer.MemberId == record.MemberId && (newer.RecordedAt > record.RecordedAt ||
                    newer.RecordedAt == record.RecordedAt && newer.Id > record.Id)))
            .OrderBy(record => record.MemberId).ToListAsync(ct);
    }

    /// <inheritdoc />
    public Task<DateTimeOffset?> FindLastSynchronizationAsync(int sessionId, CancellationToken ct = default) =>
        Context.Set<TelemetryRecord>().Where(record => record.SessionId == sessionId)
            .Select(record => (DateTimeOffset?)record.SynchronizedAt).MaxAsync(ct);

    /// <inheritdoc />
    public async Task<TelemetryRecord?> FindByClientRecordIdAsync(int sessionId, Guid clientRecordId,
        CancellationToken ct = default) =>
        Context.Set<TelemetryRecord>().Local.FirstOrDefault(record => record.SessionId == sessionId &&
            record.ClientRecordId == clientRecordId) ??
        await Context.Set<TelemetryRecord>().FirstOrDefaultAsync(record => record.SessionId == sessionId &&
            record.ClientRecordId == clientRecordId, ct);

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
