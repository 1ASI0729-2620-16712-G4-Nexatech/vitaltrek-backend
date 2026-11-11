namespace VitalTrek.API.SafetyMonitoring.Application.Errors;

/// <summary>
///     Error states that may occur while raising an early warning alert.
/// </summary>
public enum ERaiseEarlyWarningAlertError
{
    /// <summary>The alert data violates the alert invariants.</summary>
    InvalidData,

    /// <summary>The tracking session does not exist in the agency.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>The checkpoint does not belong to the route of the session.</summary>
    CheckpointNotInSessionRoute,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
