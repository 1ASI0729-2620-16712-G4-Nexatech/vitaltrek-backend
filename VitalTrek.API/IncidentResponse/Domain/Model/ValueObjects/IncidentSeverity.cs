namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Severity assigned to an incident when it is declared.
/// </summary>
public enum IncidentSeverity
{
    /// <summary>The incident does not compromise the continuity of the expedition.</summary>
    Low,

    /// <summary>The incident requires attention but the group can continue.</summary>
    Medium,

    /// <summary>The incident compromises the expedition and requires an immediate decision.</summary>
    High,

    /// <summary>The incident puts a participant at risk and requires external response.</summary>
    Critical
}

/// <summary>
///     Provides conversions between <see cref="IncidentSeverity"/> values and their external codes.
/// </summary>
public static class IncidentSeverityExtensions
{
    /// <summary>Converts the severity into its external lowercase code.</summary>
    public static string ToCode(this IncidentSeverity severity) => severity.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into an <see cref="IncidentSeverity"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static IncidentSeverity FromCode(string code) =>
        Enum.TryParse<IncidentSeverity>(code?.Trim(), true, out var severity) && Enum.IsDefined(severity)
            ? severity
            : throw new ArgumentException($"Unknown incident severity '{code}'.", nameof(code));
}
