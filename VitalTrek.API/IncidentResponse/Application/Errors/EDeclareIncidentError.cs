namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while reporting an incident.
/// </summary>
public enum EDeclareIncidentError
{
    /// <summary>The incident data violates the incident invariants.</summary>
    InvalidData,

    /// <summary>The role of the actor cannot report incidents.</summary>
    AccessForbidden,

    /// <summary>The tracking session does not exist in the actor's group scope.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>The early warning alert does not exist in the agency.</summary>
    AlertNotFound,

    /// <summary>The early warning alert belongs to another session or group.</summary>
    AlertOutsideSession,

    /// <summary>The early warning alert was dismissed and cannot originate an incident.</summary>
    AlertDismissed,

    /// <summary>Another incident was already reported from the same early warning alert.</summary>
    AlertAlreadyLinked,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
