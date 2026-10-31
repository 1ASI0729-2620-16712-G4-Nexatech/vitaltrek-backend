namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while enabling a route.
/// </summary>
public enum EEnableRouteError
{
    /// <summary>The route does not exist in the agency.</summary>
    RouteNotFound,

    /// <summary>The route has no checkpoints.</summary>
    RouteWithoutCheckpoints,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
