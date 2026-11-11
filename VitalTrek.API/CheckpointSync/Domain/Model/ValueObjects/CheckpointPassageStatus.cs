namespace VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

/// <summary>
///     Synchronization status of a checkpoint passage.
/// </summary>
public enum CheckpointPassageStatus
{
    /// <summary>The passage was synchronized within the maximum on-time delay.</summary>
    Synced,

    /// <summary>The passage was synchronized after the maximum on-time delay.</summary>
    DelayedSync
}

/// <summary>
///     Provides conversions between <see cref="CheckpointPassageStatus"/> values and their external codes.
/// </summary>
public static class CheckpointPassageStatusExtensions
{
    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this CheckpointPassageStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into a <see cref="CheckpointPassageStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static CheckpointPassageStatus FromCode(string code) =>
        Enum.TryParse<CheckpointPassageStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown checkpoint passage status '{code}'.", nameof(code));
}
