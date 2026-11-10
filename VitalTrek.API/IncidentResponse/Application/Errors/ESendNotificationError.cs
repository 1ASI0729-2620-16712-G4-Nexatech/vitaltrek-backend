namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while dispatching an incident notification.
/// </summary>
public enum ESendNotificationError
{
    /// <summary>The notification data violates the notification invariants.</summary>
    InvalidData,

    /// <summary>The incident does not exist in the agency.</summary>
    IncidentNotFound,

    /// <summary>The incident is already closed and admits no further notifications.</summary>
    IncidentClosed,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
