namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while resolving an incident.
/// </summary>
public enum EResolveIncidentError
{
    /// <summary>The incident does not exist in the agency.</summary>
    IncidentNotFound,

    /// <summary>The incident is no longer open.</summary>
    InvalidTransition,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
