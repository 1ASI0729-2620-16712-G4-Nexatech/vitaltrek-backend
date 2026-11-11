namespace VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle status of an early warning alert.
/// </summary>
public enum AlertStatus
{
    /// <summary>The alert was raised and nobody has attended it yet.</summary>
    Active,

    /// <summary>The alert was acknowledged and is being attended.</summary>
    Acknowledged,

    /// <summary>The alert was discarded as a false alarm.</summary>
    Dismissed,

    /// <summary>The risk condition was attended and closed.</summary>
    Resolved
}

/// <summary>
///     Provides conversions between <see cref="AlertStatus"/> values and their external codes.
/// </summary>
public static class AlertStatusExtensions
{
    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this AlertStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into an <see cref="AlertStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static AlertStatus FromCode(string code) =>
        Enum.TryParse<AlertStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown alert status '{code}'.", nameof(code));
}
