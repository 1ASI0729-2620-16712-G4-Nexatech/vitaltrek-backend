namespace VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

/// <summary>
///     Severity level of an early warning alert.
/// </summary>
public enum AlertSeverity
{
    /// <summary>The condition only requires attention.</summary>
    Low,

    /// <summary>The condition requires follow-up.</summary>
    Medium,

    /// <summary>The condition requires a prompt response.</summary>
    High,

    /// <summary>The condition compromises the integrity of the group.</summary>
    Critical
}

/// <summary>
///     Provides conversions between <see cref="AlertSeverity"/> values and their external codes.
/// </summary>
public static class AlertSeverityExtensions
{
    /// <summary>Converts the severity into its external lowercase code.</summary>
    public static string ToCode(this AlertSeverity severity) => severity.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into an <see cref="AlertSeverity"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static AlertSeverity FromCode(string code) =>
        Enum.TryParse<AlertSeverity>(code?.Trim(), true, out var severity) && Enum.IsDefined(severity)
            ? severity
            : throw new ArgumentException($"Unknown alert severity '{code}'.", nameof(code));
}
