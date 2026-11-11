namespace VitalTrek.API.FieldTracking.Domain.Model.Queries;

/// <summary>
///     Query to retrieve all telemetry records associated with a tracking session.
/// </summary>
public record GetTelemetryRecordsBySessionIdQuery(int AgencyId, int SessionId);
