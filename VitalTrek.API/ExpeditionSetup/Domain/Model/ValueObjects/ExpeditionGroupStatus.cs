namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle status of an expedition group.
/// </summary>
public enum ExpeditionGroupStatus
{
    /// <summary>The group is being prepared and can still be modified.</summary>
    Scheduled,

    /// <summary>The adventure tour started and the group is in the field.</summary>
    Active,

    /// <summary>The adventure tour was formally closed.</summary>
    Finished
}

/// <summary>
///     Provides conversions between <see cref="ExpeditionGroupStatus"/> values and their external codes.
/// </summary>
public static class ExpeditionGroupStatusExtensions
{
    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this ExpeditionGroupStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into an <see cref="ExpeditionGroupStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static ExpeditionGroupStatus FromCode(string code) =>
        Enum.TryParse<ExpeditionGroupStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown expedition group status '{code}'.", nameof(code));
}
