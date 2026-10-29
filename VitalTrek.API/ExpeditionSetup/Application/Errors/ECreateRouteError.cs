namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while registering a route.
/// </summary>
public enum ECreateRouteError
{
    /// <summary>The route data violates the route invariants.</summary>
    InvalidData,

    /// <summary>The agency already has a route with the same name.</summary>
    DuplicateName,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
