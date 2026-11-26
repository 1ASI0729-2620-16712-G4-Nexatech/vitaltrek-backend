namespace VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

/// <summary>
///     Stable codes of the records rejected inside a sync batch. Clients translate by code and keep the rejected
///     record visible so it can be corrected and sent in a new batch.
/// </summary>
public static class SyncRejectionCodes
{
    /// <summary>The capture time is before the session start or after the reception.</summary>
    public const string TelemetryInvalidRecordedAt = "Telemetry.InvalidRecordedAt";

    /// <summary>Latitude, longitude or altitude is missing, not finite or out of range.</summary>
    public const string TelemetryInvalidCoordinates = "Telemetry.InvalidCoordinates";

    /// <summary>The heart rate is outside the technical input range.</summary>
    public const string TelemetryInvalidHeartRate = "Telemetry.InvalidHeartRate";

    /// <summary>The battery level is outside 0 to 100.</summary>
    public const string TelemetryInvalidBattery = "Telemetry.InvalidBattery";

    /// <summary>The simulated route deviation is negative or not finite.</summary>
    public const string TelemetryInvalidDeviation = "Telemetry.InvalidDeviation";

    /// <summary>The reading was refused by the telemetry port for another data reason.</summary>
    public const string TelemetryInvalidData = "Telemetry.InvalidData";

    /// <summary>The wearable is not assigned to a participant of the session group.</summary>
    public const string TelemetryWearableNotAssigned = "Telemetry.WearableNotAssigned";

    /// <summary>The participant has not granted active monitoring consent.</summary>
    public const string TelemetryConsentRequired = "Telemetry.ConsentRequired";

    /// <summary>The passage checkpoint does not belong to the session route.</summary>
    public const string PassageCheckpointNotInRoute = "Passage.CheckpointNotInRoute";

    /// <summary>The passage time is before the session start or after the reception.</summary>
    public const string PassageInvalidPassedAt = "Passage.InvalidPassedAt";

    /// <summary>The previous checkpoint of the route has no valid earlier passage.</summary>
    public const string PassageOutOfSequence = "Passage.OutOfSequence";
}
