namespace VitalTrek.API.CheckpointSync.Application.Errors;

/// <summary>
///     Error states that reject a whole sync batch. Errors of a single record do not reject the batch; they are
///     returned as rejected records.
/// </summary>
public enum ESubmitSyncBatchError
{
    /// <summary>The batch shape is invalid: identity, size or repeated record identities.</summary>
    InvalidData,

    /// <summary>The actor is not an authenticated field guide of the agency.</summary>
    AccessForbidden,

    /// <summary>The session does not exist in the agency or the guide is not assigned to its group.</summary>
    SessionNotFound,

    /// <summary>The tracking session is not active.</summary>
    SessionNotActive,

    /// <summary>The checkpoint where the batch was synchronized does not belong to the session route.</summary>
    CheckpointNotInSessionRoute,

    /// <summary>
    ///     The checkpoint where the batch was synchronized has no valid arrival, neither persisted nor accepted in the
    ///     same batch.
    /// </summary>
    CheckpointArrivalRequired,

    /// <summary>The client batch identity was already used with a different content.</summary>
    BatchIdentityConflict,

    /// <summary>A client record identity was already used for a different capture.</summary>
    RecordIdentityConflict,

    /// <summary>Concurrent synchronization could not complete; retry with the same client batch identity.</summary>
    ConcurrentChange,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
