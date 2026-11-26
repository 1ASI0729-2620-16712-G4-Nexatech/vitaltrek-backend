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

    /// <summary>Stable device-generated identity, reused on every retry.</summary>
    public Guid ClientRecordId { get; private set; }

    /// <summary>Participant resolved by the server; unknown for historical records captured before this field existed.</summary>
    public int? MemberId { get; private set; }

    public int? HeartRate { get; private set; }
    public int? Battery { get; private set; }

    /// <summary>Simulated distance from the route in metres; not a server-computed geospatial distance.</summary>
    public double? DeviationMeters { get; private set; }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; private set; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; private set; }

    /// <summary>Gets the altitude in meters above sea level.</summary>
    public double Altitude { get; private set; }

    /// <summary>Gets the timestamp of when the device captured this measurement.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Gets the timestamp of when this record was persisted.</summary>
    public DateTimeOffset SynchronizedAt { get; private set; }

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
    public TelemetryRecord(int sessionId, int wearableDeviceId, TelemetryRecordEntry entry,
        int? memberId = null, DateTimeOffset? synchronizedAt = null)
    {
        SessionId = DomainGuard.RequirePositive(sessionId, nameof(sessionId));
        WearableDeviceId = DomainGuard.RequirePositive(wearableDeviceId, nameof(wearableDeviceId));
        if (!double.IsFinite(entry.Latitude) || entry.Latitude is < -90 or > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.", nameof(entry));
        if (!double.IsFinite(entry.Longitude) || entry.Longitude is < -180 or > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.", nameof(entry));
        if (entry.ClientRecordId == Guid.Empty || entry.HeartRate <= 0 || entry.Battery is < 0 or > 100 ||
            !double.IsFinite(entry.Altitude) || !double.IsFinite(entry.DeviationMeters) || entry.DeviationMeters < 0)
            throw new ArgumentException("Invalid telemetry identity or measurement.", nameof(entry));
        var receivedAt = NormalizeTimestamp(synchronizedAt ?? DateTimeOffset.UtcNow);
        var recordedAt = NormalizeTimestamp(entry.RecordedAt);
        if (recordedAt.Year < 1000 || recordedAt > receivedAt.AddMinutes(1))
            throw new ArgumentException("Invalid capture timestamp.", nameof(entry));
        if (memberId is not null) DomainGuard.RequirePositive(memberId.Value, nameof(memberId));
        ClientRecordId = entry.ClientRecordId;
        MemberId = memberId;
        HeartRate = entry.HeartRate;
        Battery = entry.Battery;
        DeviationMeters = entry.DeviationMeters;
        Latitude = entry.Latitude;
        Longitude = entry.Longitude;
        Altitude = entry.Altitude;
        RecordedAt = recordedAt;
        SynchronizedAt = receivedAt;
    }

    /// <summary>Compares the original capture, excluding server-generated reception time.</summary>
    public bool HasSameCapture(TelemetryRecord other) =>
        SessionId == other.SessionId && ClientRecordId == other.ClientRecordId &&
        WearableDeviceId == other.WearableDeviceId && MemberId == other.MemberId &&
        Latitude == other.Latitude && Longitude == other.Longitude && Altitude == other.Altitude &&
        RecordedAt == other.RecordedAt && HeartRate == other.HeartRate && Battery == other.Battery &&
        DeviationMeters == other.DeviationMeters;

    private static DateTimeOffset NormalizeTimestamp(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - utc.Ticks % 10, TimeSpan.Zero);
    }
}
