namespace VitalTrek.API.FieldTracking.Application.Errors;

/// <summary>
///     Error states that may occur while starting a tracking session.
/// </summary>
public enum EStartTrackingSessionError
{
    /// <summary>The actor is not an authenticated field guide in the command's agency.</summary>
    AccessForbidden,

    /// <summary>The group was not found within the field guide's scope.</summary>
    GroupNotFound,

    /// <summary>The command data violates the session invariants.</summary>
    InvalidData,

    /// <summary>The group has already started or finished without an active session to return.</summary>
    GroupNotScheduled,

    /// <summary>The route is disabled or a consecutive segment has no valid time window.</summary>
    RouteNotReady,

    /// <summary>The route has no active safety protocol.</summary>
    ProtocolNotReady,

    /// <summary>The manifest or a participant's device, consent or emergency contact is incomplete.</summary>
    ParticipantsNotReady,

    /// <summary>The group changed concurrently; the caller can retry after refreshing.</summary>
    ConcurrentChange,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
