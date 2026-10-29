namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while assigning a field guide.
/// </summary>
public enum EAssignFieldGuideError
{
    /// <summary>The group does not exist in the agency.</summary>
    ExpeditionGroupNotFound,

    /// <summary>The field guide does not exist in the agency.</summary>
    FieldGuideNotFound,

    /// <summary>The group already started or finished.</summary>
    ExpeditionGroupNotScheduled,

    /// <summary>The field guide is assigned to another group on the same date and no confirmation was given.</summary>
    ScheduleConflict,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
