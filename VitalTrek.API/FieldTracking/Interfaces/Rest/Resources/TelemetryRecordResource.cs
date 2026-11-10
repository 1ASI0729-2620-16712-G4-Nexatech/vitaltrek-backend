namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a single telemetry measurement returned to clients.
/// </summary>
public record TelemetryRecordResource(
    int Id,
    int SessionId,
    int WearableDeviceId,
    double Latitude,
    double Longitude,
    double Altitude,
    DateTimeOffset RecordedAt,
    DateTimeOffset SyncedAt);
