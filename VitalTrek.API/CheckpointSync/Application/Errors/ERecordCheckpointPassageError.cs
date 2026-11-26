namespace VitalTrek.API.CheckpointSync.Application.Errors;

/// <summary>
///     Error states that may occur while recording a checkpoint passage.
/// </summary>
public enum ERecordCheckpointPassageError
{
    /// <summary>The passage data violates the passage invariants.</summary>
    InvalidData,

    /// <summary>The actor is not the authenticated field guide recorded in the passage.</summary>
    AccessForbidden,

    /// <summary>The tracking session does not exist in the agency or the guide is not assigned to its group.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>The checkpoint does not belong to the route of the session.</summary>
    CheckpointNotInSessionRoute,

    /// <summary>The passage time is before the session start or after its synchronization.</summary>
    InvalidPassedAt,

    /// <summary>The previous checkpoint of the route has no valid passage at or before this passage time.</summary>
    OutOfSequence,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
