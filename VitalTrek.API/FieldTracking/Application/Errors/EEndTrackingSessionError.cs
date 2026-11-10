namespace VitalTrek.API.FieldTracking.Application.Errors;

/// <summary>
///     Error states that may occur while ending a tracking session.
/// </summary>
public enum EEndTrackingSessionError
{
    /// <summary>The session was not found or does not belong to the agency.</summary>
    SessionNotFound,

    /// <summary>The session is not in an active state and cannot be ended.</summary>
    SessionNotActive,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
