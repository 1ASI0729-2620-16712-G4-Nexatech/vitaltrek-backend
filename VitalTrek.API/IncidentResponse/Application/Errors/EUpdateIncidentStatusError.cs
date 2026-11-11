namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while changing the status of an incident.
/// </summary>
public enum EUpdateIncidentStatusError
{
    /// <summary>The target status is not a known incident status.</summary>
    InvalidData,

    /// <summary>The incident does not exist in the agency.</summary>
    IncidentNotFound,

    /// <summary>The transition is not allowed from the current status.</summary>
    InvalidTransition,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
