namespace VitalTrek.API.FieldTracking.Application.Errors;

/// <summary>
///     Error states that may occur while starting a tracking session.
/// </summary>
public enum EStartTrackingSessionError
{
    /// <summary>The command data violates the session invariants.</summary>
    InvalidData,

    /// <summary>An active session already exists for this expedition group.</summary>
    SessionAlreadyActive,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
