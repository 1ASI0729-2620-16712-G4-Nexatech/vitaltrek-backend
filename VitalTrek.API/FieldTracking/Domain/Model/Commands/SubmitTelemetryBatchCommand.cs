namespace VitalTrek.API.FieldTracking.Domain.Model.Commands;

/// <summary>
///     Command to persist a batch of telemetry records for a wearable device.
/// </summary>
public record SubmitTelemetryBatchCommand(
    int AgencyId,
    int SessionId,
    int WearableDeviceId,
    IReadOnlyList<TelemetryRecordEntry> Records);

/// <summary>
///     A single GPS measurement within a telemetry batch.
/// </summary>
public record TelemetryRecordEntry(
    double Latitude,
    double Longitude,
    double Altitude,
    DateTimeOffset RecordedAt);
