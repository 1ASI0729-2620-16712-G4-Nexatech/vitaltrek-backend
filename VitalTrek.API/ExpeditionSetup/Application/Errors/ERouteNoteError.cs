namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while saving a route note.
/// </summary>
public enum ERouteNoteError
{
    /// <summary>The note data violates the note invariants.</summary>
    InvalidData,

    /// <summary>The route does not exist in the agency.</summary>
    RouteNotFound,

    /// <summary>The note does not exist in the agency.</summary>
    RouteNoteNotFound,

    /// <summary>The route already has a note in the same display position.</summary>
    DuplicateDisplayOrder,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
