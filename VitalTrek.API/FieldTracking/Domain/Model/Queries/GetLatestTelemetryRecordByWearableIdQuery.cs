namespace VitalTrek.API.FieldTracking.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the most recent telemetry record for a specific wearable device within a session.
/// </summary>
public record GetLatestTelemetryRecordByWearableIdQuery(int AgencyId, int SessionId, int WearableDeviceId);
