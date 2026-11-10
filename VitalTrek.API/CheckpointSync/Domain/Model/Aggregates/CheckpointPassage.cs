using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the passage of a field guide through a physical checkpoint,
///     synchronized over Bluetooth.
/// </summary>
public partial class CheckpointPassage
{
    /// <summary>Maximum delay between the passage and its synchronization to be considered on time.</summary>
    public static readonly TimeSpan MaximumOnTimeSyncDelay = TimeSpan.FromMinutes(30);

    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    /// <summary>Gets the passage identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the tracking session the passage belongs to.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the checkpoint that was passed.</summary>
    public int CheckpointId { get; private set; }

    /// <summary>Gets the field guide who passed the checkpoint.</summary>
    public int FieldGuideId { get; private set; }

    /// <summary>Gets the agency that owns the passage.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the moment the checkpoint was passed.</summary>
    public DateTimeOffset PassedAt { get; private set; }

    /// <summary>Gets the moment the passage was synchronized.</summary>
    public DateTimeOffset SyncedAt { get; private set; }

    /// <summary>Gets the synchronization status.</summary>
    public CheckpointPassageStatus Status { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CheckpointPassage"/> class for Entity Framework Core.
    /// </summary>
    public CheckpointPassage()
    {
    }

    /// <summary>
    ///     Initializes a new checkpoint passage.
    /// </summary>
    /// <param name="command">The record checkpoint passage command.</param>
    /// <param name="syncedAt">The synchronization moment.</param>
    /// <exception cref="ArgumentException">Thrown when the passage data is invalid.</exception>
    public CheckpointPassage(RecordCheckpointPassageCommand command, DateTimeOffset syncedAt)
    {
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        CheckpointId = DomainGuard.RequirePositive(command.CheckpointId, nameof(command.CheckpointId));
        FieldGuideId = DomainGuard.RequirePositive(command.FieldGuideId, nameof(command.FieldGuideId));
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        if (command.PassedAt > syncedAt + ClockTolerance)
            throw new ArgumentException("PassedAt cannot be later than the synchronization moment.", nameof(command));
        PassedAt = command.PassedAt;
        SyncedAt = syncedAt;
        Status = SyncedAt - PassedAt > MaximumOnTimeSyncDelay
            ? CheckpointPassageStatus.DelayedSync
            : CheckpointPassageStatus.Synced;
    }
}
