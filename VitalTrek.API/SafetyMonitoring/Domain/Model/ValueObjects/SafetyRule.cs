namespace VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

/// <summary>
///     Rule that produced an early warning alert. Automatic rules evaluate persisted data on the server; manual alerts
///     are reported by the operation.
/// </summary>
public enum SafetyRule
{
    /// <summary>The alert was reported manually by the operation.</summary>
    Manual,

    /// <summary>A persisted heart rate reading is outside the baseline range of the participant.</summary>
    HeartRateOutsideBaseline,

    /// <summary>A persisted, simulated route deviation exceeds the demonstration threshold.</summary>
    SimulatedRouteDeviation,

    /// <summary>The group has not reached the next checkpoint within its expected time window.</summary>
    CheckpointDelay
}

/// <summary>
///     Provides conversions between <see cref="SafetyRule"/> values, their external codes and the alert type they raise.
/// </summary>
public static class SafetyRuleExtensions
{
    private static readonly Dictionary<SafetyRule, string> Codes = new()
    {
        [SafetyRule.Manual] = "manual",
        [SafetyRule.HeartRateOutsideBaseline] = "heart_rate_outside_baseline",
        [SafetyRule.SimulatedRouteDeviation] = "simulated_route_deviation",
        [SafetyRule.CheckpointDelay] = "checkpoint_delay"
    };

    /// <summary>Converts the safety rule into its external snake_case code.</summary>
    public static string ToCode(this SafetyRule rule) => Codes[rule];

    /// <summary>Converts an external code into a <see cref="SafetyRule"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static SafetyRule FromCode(string code)
    {
        foreach (var (rule, value) in Codes)
            if (string.Equals(value, code?.Trim(), StringComparison.OrdinalIgnoreCase)) return rule;
        throw new ArgumentException($"Unknown safety rule '{code}'.", nameof(code));
    }

    /// <summary>Returns the alert type raised by an automatic safety rule.</summary>
    /// <exception cref="ArgumentException">Thrown for manual alerts, whose type is chosen by the operation.</exception>
    public static AlertType ToAlertType(this SafetyRule rule) => rule switch
    {
        SafetyRule.HeartRateOutsideBaseline => AlertType.VitalSignAnomaly,
        SafetyRule.SimulatedRouteDeviation => AlertType.RouteDeviation,
        SafetyRule.CheckpointDelay => AlertType.Delay,
        _ => throw new ArgumentException("Only an automatic safety rule determines the alert type.", nameof(rule))
    };
}
