namespace VitalTrek.API.CheckpointSync.Application.Errors;

/// <summary>
///     Error states that may occur while recording a checkpoint passage.
/// </summary>
public enum ERecordCheckpointPassageError
{
    /// <summary>The passage data violates the passage invariants.</summary>
    InvalidData,

    /// <summary>The tracking session does not exist in the agency.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>The checkpoint does not belong to the route of the session.</summary>
    CheckpointNotInSessionRoute,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
