namespace VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;

/// <summary>
///     Persisted reading exposed to other bounded contexts. RecordedAt is the original capture and SynchronizedAt the
///     reception; DeviationMeters is a simulated input.
/// </summary>
public sealed record TelemetryReadingSnapshot(Guid ClientRecordId, int? MemberId, int WearableDeviceId,
    DateTimeOffset RecordedAt, DateTimeOffset SynchronizedAt, double Latitude, double Longitude, double Altitude,
    int? HeartRate, int? Battery, double? DeviationMeters);
