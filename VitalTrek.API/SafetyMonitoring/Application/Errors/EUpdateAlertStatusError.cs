namespace VitalTrek.API.SafetyMonitoring.Application.Errors;

/// <summary>
///     Error states that may occur while acknowledging, dismissing or resolving an early warning alert.
/// </summary>
public enum EUpdateAlertStatusError
{
    /// <summary>The alert does not exist in the agency.</summary>
    AlertNotFound,

    /// <summary>The current status of the alert does not allow the requested transition.</summary>
    InvalidTransition,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
