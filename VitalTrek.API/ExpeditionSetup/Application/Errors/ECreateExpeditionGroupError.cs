namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while creating an expedition group.
/// </summary>
public enum ECreateExpeditionGroupError
{
    /// <summary>The group data is invalid.</summary>
    InvalidData,

    /// <summary>The route does not exist in the agency.</summary>
    RouteNotFound,

    /// <summary>The route is not enabled for tour planning.</summary>
    RouteNotEnabled,

    /// <summary>The departure date is earlier than today.</summary>
    DepartureDateInPast,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
