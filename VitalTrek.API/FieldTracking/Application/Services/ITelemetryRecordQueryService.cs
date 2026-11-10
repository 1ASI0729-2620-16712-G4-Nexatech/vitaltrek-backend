using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application query service interface for telemetry record read operations.
/// </summary>
public interface ITelemetryRecordQueryService
{
    /// <summary>Retrieves all telemetry records for a session.</summary>
    Task<IEnumerable<TelemetryRecord>> Handle(GetTelemetryRecordsBySessionIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the most recent telemetry record for a wearable device within a session.</summary>
    Task<TelemetryRecord?> Handle(GetLatestTelemetryRecordByWearableIdQuery query, CancellationToken cancellationToken);
}
