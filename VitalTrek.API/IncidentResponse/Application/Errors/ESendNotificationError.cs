namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while requesting, retrying or refreshing an incident notification.
/// </summary>
public enum ESendNotificationError
{
    /// <summary>The notification data violates the notification invariants.</summary>
    InvalidData,

    /// <summary>Only an operations administrator requests external communications.</summary>
    AccessForbidden,

    /// <summary>The incident does not exist in the agency.</summary>
    IncidentNotFound,

    /// <summary>External communications require a declared emergency.</summary>
    EmergencyNotDeclared,

    /// <summary>The recipient is not an authorized staff address of the agency.</summary>
    RecipientNotAllowed,

    /// <summary>The notification does not exist for the incident.</summary>
    NotificationNotFound,

    /// <summary>The notification identity was already used with different content.</summary>
    IdentityConflict,

    /// <summary>Another attempt of the same notification is in progress.</summary>
    AttemptInProgress,

    /// <summary>Only a pending notification can be retried.</summary>
    NotRetriable,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
