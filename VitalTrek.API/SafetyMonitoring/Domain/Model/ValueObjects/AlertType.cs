namespace VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

/// <summary>
///     Type of condition that raised an early warning alert.
/// </summary>
public enum AlertType
{
    /// <summary>The group did not reach a checkpoint within the expected time window.</summary>
    Delay,

    /// <summary>A vital sign reading is outside the baseline range.</summary>
    VitalSignAnomaly,

    /// <summary>The group deviated from the configured route.</summary>
    RouteDeviation,

    /// <summary>Any other risk condition reported by the operation.</summary>
    Other
}

/// <summary>
///     Provides conversions between <see cref="AlertType"/> values and their external codes.
/// </summary>
public static class AlertTypeExtensions
{
    private static readonly Dictionary<AlertType, string> Codes = new()
    {
        [AlertType.Delay] = "delay",
        [AlertType.VitalSignAnomaly] = "vital_sign_anomaly",
        [AlertType.RouteDeviation] = "route_deviation",
        [AlertType.Other] = "other"
    };

    /// <summary>Converts the alert type into its external snake_case code.</summary>
    public static string ToCode(this AlertType type) => Codes[type];

    /// <summary>Converts an external code into an <see cref="AlertType"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static AlertType FromCode(string code)
    {
        foreach (var (type, value) in Codes)
            if (string.Equals(value, code?.Trim(), StringComparison.OrdinalIgnoreCase)) return type;
        throw new ArgumentException($"Unknown alert type '{code}'.", nameof(code));
    }
}
