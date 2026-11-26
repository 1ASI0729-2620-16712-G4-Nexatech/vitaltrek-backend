using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

namespace VitalTrek.API.CheckpointSync.Domain.Services;

/// <summary>
///     Technical input rules of an offline reading. They only reject impossible inputs; clinical thresholds belong to
///     Safety Monitoring, so a reading outside a participant baseline is still accepted and evaluated later.
/// </summary>
public static class SyncReadingRules
{
    /// <summary>Lowest heart rate accepted as a technical input, in beats per minute.</summary>
    public const int MinimumHeartRate = 1;

    /// <summary>Highest heart rate accepted as a technical input, in beats per minute.</summary>
    public const int MaximumHeartRate = 300;

    /// <summary>Clock difference tolerated between the device and the server.</summary>
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    /// <summary>Returns the rejection code of the first invalid value, or null when the reading is valid.</summary>
    /// <param name="reading">The reading to check.</param>
    /// <param name="sessionStartedAt">The start of the tracking session.</param>
    /// <param name="receivedAt">The reception moment of the batch.</param>
    public static string? RejectionCodeOf(SyncBatchReadingEntry reading, DateTimeOffset sessionStartedAt,
        DateTimeOffset receivedAt)
    {
        if (reading.RecordedAt < sessionStartedAt - ClockTolerance || reading.RecordedAt > receivedAt + ClockTolerance)
            return SyncRejectionCodes.TelemetryInvalidRecordedAt;
        if (!double.IsFinite(reading.Latitude) || reading.Latitude is < -90 or > 90 ||
            !double.IsFinite(reading.Longitude) || reading.Longitude is < -180 or > 180 ||
            !double.IsFinite(reading.Altitude))
            return SyncRejectionCodes.TelemetryInvalidCoordinates;
        if (reading.HeartRate is < MinimumHeartRate or > MaximumHeartRate)
            return SyncRejectionCodes.TelemetryInvalidHeartRate;
        if (reading.Battery is < 0 or > 100) return SyncRejectionCodes.TelemetryInvalidBattery;
        if (!double.IsFinite(reading.DeviationMeters) || reading.DeviationMeters < 0)
            return SyncRejectionCodes.TelemetryInvalidDeviation;
        return null;
    }
}
