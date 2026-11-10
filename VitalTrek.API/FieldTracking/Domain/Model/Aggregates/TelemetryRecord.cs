using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.FieldTracking.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a single GPS measurement submitted by a wearable device.
/// </summary>
public partial class TelemetryRecord
{
    /// <summary>Gets the record identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the tracking session this record belongs to.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the wearable device that generated this measurement.</summary>
    public int WearableDeviceId { get; private set; }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; private set; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; private set; }

    /// <summary>Gets the altitude in meters above sea level.</summary>
    public double Altitude { get; private set; }

    /// <summary>Gets the timestamp of when the device captured this measurement.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Gets the timestamp of when this record was persisted.</summary>
    public DateTimeOffset SyncedAt { get; private set; }

    /// <summary>
    ///     Initializes a new instance of <see cref="TelemetryRecord"/> for Entity Framework Core.
    /// </summary>
    public TelemetryRecord()
    {
    }

    /// <summary>
    ///     Creates a telemetry record from a batch entry.
    /// </summary>
    /// <param name="sessionId">The session that owns this record.</param>
    /// <param name="wearableDeviceId">The device that produced the measurement.</param>
    /// <param name="entry">The raw measurement values.</param>
    /// <exception cref="ArgumentException">Thrown when any value is out of range.</exception>
    public TelemetryRecord(int sessionId, int wearableDeviceId, TelemetryRecordEntry entry)
    {
        SessionId = DomainGuard.RequirePositive(sessionId, nameof(sessionId));
        WearableDeviceId = DomainGuard.RequirePositive(wearableDeviceId, nameof(wearableDeviceId));
        if (entry.Latitude is < -90 or > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.", nameof(entry));
        if (entry.Longitude is < -180 or > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.", nameof(entry));
        Latitude = entry.Latitude;
        Longitude = entry.Longitude;
        Altitude = entry.Altitude;
        RecordedAt = entry.RecordedAt;
        SyncedAt = DateTimeOffset.UtcNow;
    }
}
