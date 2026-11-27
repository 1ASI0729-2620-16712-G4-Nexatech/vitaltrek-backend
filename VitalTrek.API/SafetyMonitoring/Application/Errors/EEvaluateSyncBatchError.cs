namespace VitalTrek.API.SafetyMonitoring.Application.Errors;

/// <summary>
///     Error states that may occur while evaluating the readings accepted by a persisted sync batch. Every failure
///     keeps the batch pending, so its evaluation can be retried without the client sending the readings again.
/// </summary>
public enum EEvaluateSyncBatchError
{
    /// <summary>The batch does not exist in the agency.</summary>
    BatchNotFound,

    /// <summary>The tracking session of the batch does not exist in the agency.</summary>
    SessionNotFound,

    /// <summary>Another evaluation stored the same evidence first; the batch stays pending for a retry.</summary>
    ConcurrentEvaluation,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
