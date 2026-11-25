using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application query service interface for telemetry record read operations.
/// </summary>
public interface ITelemetryRecordQueryService
{
    /// <summary>Retrieves session telemetry, or null when the session is outside the actor's scope.</summary>
    Task<IEnumerable<TelemetryRecord>?> Handle(GetTelemetryRecordsBySessionIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Retrieves the most recent telemetry record for a wearable device within a session.</summary>
    Task<TelemetryRecord?> Handle(GetLatestTelemetryRecordByWearableIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);
}
