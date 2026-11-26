using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

namespace VitalTrek.API.CheckpointSync.Domain.Model.Entities;

/// <summary>
///     Persisted outcome of one record of a sync batch, kept so that repeating the batch returns exactly its result.
/// </summary>
public class SyncBatchRecord
{
    /// <summary>Maximum length of a rejection code.</summary>
    public const int MaximumRejectionCodeLength = 64;

    /// <summary>Gets the record result identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the sync batch that owns this result.</summary>
    public int SyncBatchId { get; private set; }

    /// <summary>Gets the stable device-generated identity of the record.</summary>
    public Guid ClientRecordId { get; private set; }

    /// <summary>Gets the kind of record.</summary>
    public SyncRecordKind Kind { get; private set; }

    /// <summary>Gets the processing outcome.</summary>
    public SyncRecordOutcome Outcome { get; private set; }

    /// <summary>Gets the stable rejection code; only present when the record was rejected.</summary>
    public string? RejectionCode { get; private set; }

    /// <summary>Gets the fingerprint of the record content, used to detect a reused identity with different data.</summary>
    public string RecordFingerprint { get; private set; } = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SyncBatchRecord"/> class for Entity Framework Core.
    /// </summary>
    public SyncBatchRecord()
    {
    }

    /// <summary>
    ///     Initializes a new record result.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the identity, fingerprint or rejection code is invalid.</exception>
    internal SyncBatchRecord(Guid clientRecordId, SyncRecordKind kind, SyncRecordOutcome outcome, string? rejectionCode,
        string recordFingerprint)
    {
        if (clientRecordId == Guid.Empty)
            throw new ArgumentException("ClientRecordId is required.", nameof(clientRecordId));
        if (recordFingerprint is not { Length: Aggregates.SyncBatch.FingerprintLength } ||
            !recordFingerprint.All(Uri.IsHexDigit))
            throw new ArgumentException("The record fingerprint must be a SHA-256 hexadecimal value.",
                nameof(recordFingerprint));
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Unknown record kind.", nameof(kind));
        if (!Enum.IsDefined(outcome)) throw new ArgumentException("Unknown record outcome.", nameof(outcome));
        if (outcome == SyncRecordOutcome.Rejected)
        {
            if (string.IsNullOrWhiteSpace(rejectionCode) || rejectionCode.Length > MaximumRejectionCodeLength)
                throw new ArgumentException("A rejected record requires a stable code.", nameof(rejectionCode));
        }
        else if (rejectionCode is not null)
            throw new ArgumentException("Only rejected records carry a code.", nameof(rejectionCode));

        ClientRecordId = clientRecordId;
        Kind = kind;
        Outcome = outcome;
        RejectionCode = rejectionCode;
        RecordFingerprint = recordFingerprint.ToLowerInvariant();
    }
}
