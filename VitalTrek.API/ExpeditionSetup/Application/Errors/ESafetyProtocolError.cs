namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while saving a safety protocol.
/// </summary>
public enum ESafetyProtocolError
{
    /// <summary>The protocol data violates the protocol invariants.</summary>
    InvalidData,

    /// <summary>The route does not exist in the agency.</summary>
    RouteNotFound,

    /// <summary>The protocol does not exist in the agency.</summary>
    SafetyProtocolNotFound,

    /// <summary>The route already has a safety protocol.</summary>
    DuplicateProtocol,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
