namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while executing a transition of the incident lifecycle.
/// </summary>
public enum EUpdateIncidentStatusError
{
    /// <summary>The target status or the note is invalid.</summary>
    InvalidData,

    /// <summary>The role of the actor cannot execute the transition.</summary>
    AccessForbidden,

    /// <summary>The incident does not exist in the actor's group scope.</summary>
    IncidentNotFound,

    /// <summary>The transition is not allowed from the current status.</summary>
    InvalidTransition,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
