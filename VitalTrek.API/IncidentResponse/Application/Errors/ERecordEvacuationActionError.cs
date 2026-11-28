namespace VitalTrek.API.IncidentResponse.Application.Errors;

/// <summary>
///     Error states that may occur while recording an evacuation action.
/// </summary>
public enum ERecordEvacuationActionError
{
    /// <summary>The action data violates the evacuation action invariants.</summary>
    InvalidData,

    /// <summary>Only the assigned field guide records evacuation actions.</summary>
    AccessForbidden,

    /// <summary>The incident does not exist in the actor's group scope.</summary>
    IncidentNotFound,

    /// <summary>The capture does not belong to a declared emergency of the incident.</summary>
    EmergencyNotDeclared,

    /// <summary>The client action identity was already used with different content.</summary>
    ActionIdentityConflict,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
