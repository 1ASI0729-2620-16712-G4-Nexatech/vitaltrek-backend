namespace VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

/// <summary>
///     Kind of offline event carried by a sync batch.
/// </summary>
public enum SyncRecordKind
{
    /// <summary>A wearable telemetry reading.</summary>
    Reading,

    /// <summary>A checkpoint passage of the field guide.</summary>
    Passage
}

/// <summary>
///     Result of processing one record of a sync batch.
/// </summary>
public enum SyncRecordOutcome
{
    /// <summary>The record was stored by this batch.</summary>
    Accepted,

    /// <summary>The record was already stored; the first evidence is kept.</summary>
    Duplicate,

    /// <summary>The record was not stored and carries a rejection code.</summary>
    Rejected
}

/// <summary>
///     Safety evaluation state of the readings accepted by a sync batch. Batches are stored as pending; Safety
///     Monitoring (D01 and D02) marks them evaluated through the Checkpoint Sync facade once its alerts are saved.
/// </summary>
public enum SyncBatchEvaluationStatus
{
    /// <summary>The accepted readings still have to be evaluated.</summary>
    Pending,

    /// <summary>The accepted readings were evaluated and the result was saved.</summary>
    Evaluated
}

/// <summary>
///     Conversions between the sync batch enumerations and their external lowercase codes.
/// </summary>
public static class SyncBatchCodeExtensions
{
    /// <summary>Converts the kind into its external lowercase code.</summary>
    public static string ToCode(this SyncRecordKind kind) => kind.ToString().ToLowerInvariant();

    /// <summary>Converts the outcome into its external lowercase code.</summary>
    public static string ToCode(this SyncRecordOutcome outcome) => outcome.ToString().ToLowerInvariant();

    /// <summary>Converts the evaluation status into its external lowercase code.</summary>
    public static string ToCode(this SyncBatchEvaluationStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Parses a stored code into a <typeparamref name="TEnum"/> value.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static TEnum FromCode<TEnum>(string code) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(code?.Trim(), true, out var value) && Enum.IsDefined(value)
            ? value
            : throw new ArgumentException($"Unknown {typeof(TEnum).Name} code '{code}'.", nameof(code));
}
