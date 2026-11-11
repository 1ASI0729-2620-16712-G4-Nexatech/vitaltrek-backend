namespace VitalTrek.API.SafetyMonitoring.Application.Errors;

/// <summary>
///     Error states that may occur while evaluating the delays of a tracking session.
/// </summary>
public enum EEvaluateSessionDelaysError
{
    /// <summary>The tracking session does not exist in the agency.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
