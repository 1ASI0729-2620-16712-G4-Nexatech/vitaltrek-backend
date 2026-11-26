namespace VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;

/// <summary>Cross-context capture contract. Reception time and participant identity are resolved by the server.</summary>
public sealed record TelemetryReadingInput(Guid ClientRecordId, int WearableDeviceId, DateTimeOffset RecordedAt,
    double Latitude, double Longitude, double Altitude, int HeartRate, int Battery, double DeviationMeters);

public enum TelemetryReadingStageStatus
{
    Accepted, Duplicate, AccessForbidden, InvalidData, SessionNotFound, SessionNotActive,
    WearableNotAssigned, ConsentRequired, RecordIdentityConflict, ConcurrentChange, UnexpectedError
}

/// <summary>Accepted means staged, not committed. Sync Batch must persist all accepted readings and its batch once.</summary>
public sealed record TelemetryReadingStageResult(Guid ClientRecordId, TelemetryReadingStageStatus Status);
