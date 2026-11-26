using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.Entities;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a batch of offline readings and passages received at a checkpoint.
///     A batch is stored in the same transaction as its accepted readings and passages, so a persisted batch is
///     always a completed one; a storage failure leaves no batch behind.
/// </summary>
public partial class SyncBatch
{
    /// <summary>Maximum number of readings and passages accepted in one batch.</summary>
    public const int MaximumRecords = 500;

    /// <summary>Length of the SHA-256 hexadecimal content fingerprint.</summary>
    public const int FingerprintLength = 64;

    private readonly List<SyncBatchRecord> _records = [];

    /// <summary>Gets the batch identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the batch.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the tracking session the batch belongs to.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the checkpoint where the batch was synchronized.</summary>
    public int CheckpointId { get; private set; }

    /// <summary>Gets the field guide who submitted the batch.</summary>
    public int FieldGuideId { get; private set; }

    /// <summary>Gets the stable device-generated batch identity.</summary>
    public Guid ClientBatchId { get; private set; }

    /// <summary>Gets the fingerprint of the batch content, used to detect a reused identity with different data.</summary>
    public string ContentFingerprint { get; private set; } = string.Empty;

    /// <summary>Gets the moment the server received the batch.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>Gets the evaluation status of the accepted readings.</summary>
    public SyncBatchEvaluationStatus EvaluationStatus { get; private set; }

    /// <summary>Gets the outcome of every record in the batch.</summary>
    public IReadOnlyCollection<SyncBatchRecord> Records => _records;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SyncBatch"/> class for Entity Framework Core.
    /// </summary>
    public SyncBatch()
    {
    }

    /// <summary>
    ///     Initializes a new sync batch whose evaluation is pending.
    /// </summary>
    /// <param name="command">The submit sync batch command.</param>
    /// <param name="contentFingerprint">The fingerprint of the batch content.</param>
    /// <param name="receivedAt">The reception moment.</param>
    /// <exception cref="ArgumentException">Thrown when the batch data is invalid.</exception>
    public SyncBatch(SubmitSyncBatchCommand command, string contentFingerprint, DateTimeOffset receivedAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        FieldGuideId = DomainGuard.RequirePositive(command.FieldGuideId, nameof(command.FieldGuideId));
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        CheckpointId = DomainGuard.RequirePositive(command.CheckpointId, nameof(command.CheckpointId));
        if (command.ClientBatchId == Guid.Empty)
            throw new ArgumentException("ClientBatchId is required.", nameof(command));
        if (contentFingerprint is not { Length: FingerprintLength } || !contentFingerprint.All(Uri.IsHexDigit))
            throw new ArgumentException("The content fingerprint must be a SHA-256 hexadecimal value.",
                nameof(contentFingerprint));
        ClientBatchId = command.ClientBatchId;
        ContentFingerprint = contentFingerprint.ToLowerInvariant();
        // Stored with microsecond precision, so the first response and later reads expose the same value.
        var utc = receivedAt.ToUniversalTime();
        ReceivedAt = new DateTimeOffset(utc.Ticks - utc.Ticks % 10, TimeSpan.Zero);
        EvaluationStatus = SyncBatchEvaluationStatus.Pending;
    }

    /// <summary>Returns whether a submission carries exactly the content this batch was created from.</summary>
    public bool HasSameContent(string contentFingerprint) =>
        string.Equals(ContentFingerprint, contentFingerprint, StringComparison.OrdinalIgnoreCase);

    /// <summary>Registers a record stored by this batch.</summary>
    public void Accept(Guid clientRecordId, SyncRecordKind kind) =>
        Add(new SyncBatchRecord(clientRecordId, kind, SyncRecordOutcome.Accepted, null));

    /// <summary>Registers a record that was already stored before; the first evidence is kept.</summary>
    public void MarkDuplicate(Guid clientRecordId, SyncRecordKind kind) =>
        Add(new SyncBatchRecord(clientRecordId, kind, SyncRecordOutcome.Duplicate, null));

    /// <summary>Registers a record that was not stored, with its stable rejection code.</summary>
    public void Reject(Guid clientRecordId, SyncRecordKind kind, string code) =>
        Add(new SyncBatchRecord(clientRecordId, kind, SyncRecordOutcome.Rejected, code));

    /// <summary>Returns the identities with the given outcome, in a stable order.</summary>
    public IReadOnlyList<Guid> RecordIdsWith(SyncRecordOutcome outcome) =>
        _records.Where(record => record.Outcome == outcome).Select(record => record.ClientRecordId)
            .Order().ToArray();

    /// <summary>Returns the rejected records, in a stable order.</summary>
    public IReadOnlyList<SyncBatchRecord> RejectedRecords() =>
        _records.Where(record => record.Outcome == SyncRecordOutcome.Rejected)
            .OrderBy(record => record.ClientRecordId).ToArray();

    private void Add(SyncBatchRecord record)
    {
        if (_records.Any(existing => existing.ClientRecordId == record.ClientRecordId))
            throw new InvalidOperationException("A record can only have one outcome in a batch.");
        if (_records.Count >= MaximumRecords)
            throw new InvalidOperationException($"A batch cannot contain more than {MaximumRecords} records.");
        _records.Add(record);
    }
}
