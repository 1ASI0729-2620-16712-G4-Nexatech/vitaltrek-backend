using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.FieldTracking.Domain.Repositories;

/// <summary>
///     Repository contract for <see cref="TelemetryRecord"/> persistence.
/// </summary>
public interface ITelemetryRecordRepository : IBaseRepository<TelemetryRecord>
{
    /// <summary>Gets the latest capture per allowed participant, filtering participant scope in SQL.</summary>
    Task<IReadOnlyList<TelemetryRecord>> FindLatestForMembersAsync(int sessionId, IReadOnlyCollection<int> memberIds,
        CancellationToken ct = default);

    /// <summary>Gets only the last persisted reception time, without exposing other participants' measurements.</summary>
    Task<DateTimeOffset?> FindLastSynchronizationAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Finds the stable reading identity, including records already staged in this unit of work.</summary>
    Task<TelemetryRecord?> FindByClientRecordIdAsync(int sessionId, Guid clientRecordId, CancellationToken ct = default);

    /// <summary>Retrieves the persisted records of a session with the given stable identities, without tracking.</summary>
    Task<IReadOnlyList<TelemetryRecord>> FindByClientRecordIdsAsync(int sessionId, IReadOnlyCollection<Guid> clientRecordIds,
        CancellationToken ct = default);

    /// <summary>Retrieves all telemetry records for a session.</summary>
    Task<IEnumerable<TelemetryRecord>> FindAllBySessionIdAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Retrieves the most recent telemetry record for a wearable device within a session.</summary>
    Task<TelemetryRecord?> FindLatestBySessionIdAndWearableIdAsync(int sessionId, int wearableDeviceId, CancellationToken ct = default);

    /// <summary>Adds a collection of records in a single batch.</summary>
    Task AddRangeAsync(IEnumerable<TelemetryRecord> records, CancellationToken ct = default);
}
