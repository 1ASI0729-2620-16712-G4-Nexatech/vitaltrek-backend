namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while declaring an incident.
/// </summary>
public enum EDeclareIncidentError
{
    /// <summary>The incident data violates the incident invariants.</summary>
    InvalidData,

    /// <summary>The tracking session does not exist in the agency.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
