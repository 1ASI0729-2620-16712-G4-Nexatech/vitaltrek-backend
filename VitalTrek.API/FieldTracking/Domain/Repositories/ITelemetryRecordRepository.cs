using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.FieldTracking.Domain.Repositories;

/// <summary>
///     Repository contract for <see cref="TelemetryRecord"/> persistence.
/// </summary>
public interface ITelemetryRecordRepository : IBaseRepository<TelemetryRecord>
{
    /// <summary>Retrieves all telemetry records for a session.</summary>
    Task<IEnumerable<TelemetryRecord>> FindAllBySessionIdAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Retrieves the most recent telemetry record for a wearable device within a session.</summary>
    Task<TelemetryRecord?> FindLatestBySessionIdAndWearableIdAsync(int sessionId, int wearableDeviceId, CancellationToken ct = default);

    /// <summary>Adds a collection of records in a single batch.</summary>
    Task AddRangeAsync(IEnumerable<TelemetryRecord> records, CancellationToken ct = default);
}
