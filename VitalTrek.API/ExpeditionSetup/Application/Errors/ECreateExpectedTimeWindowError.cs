namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while setting an expected time window.
/// </summary>
public enum ECreateExpectedTimeWindowError
{
    /// <summary>The limits are inconsistent or the data is invalid.</summary>
    InvalidData,

    /// <summary>The route does not exist in the agency.</summary>
    RouteNotFound,

    /// <summary>The checkpoints do not form a segment of consecutive checkpoints of the route.</summary>
    SegmentNotFound,

    /// <summary>The segment already has an expected time window.</summary>
    DuplicateSegment,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
