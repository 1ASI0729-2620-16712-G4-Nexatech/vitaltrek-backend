using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

namespace VitalTrek.API.SafetyMonitoring.Domain.Services;

/// <summary>
///     Demonstration rules of the academic simulation, evaluated over readings already persisted by the server. They
///     are not a medical assessment: the heart rate limits come from the participant baseline and the route deviation
///     is a simulated input compared against a demonstration threshold.
/// </summary>
public static class DemonstrationSafetyRules
{
    /// <summary>Demonstration threshold of the simulated route deviation, in meters. Only greater values raise.</summary>
    public const int RouteDeviationThresholdMeters = 100;

    /// <summary>
    ///     Evaluates a heart rate against the baseline of the participant. Both limits belong to the baseline.
    /// </summary>
    /// <param name="heartRate">The heart rate, in beats per minute.</param>
    /// <param name="baselineMin">The lowest heart rate of the baseline.</param>
    /// <param name="baselineMax">The highest heart rate of the baseline.</param>
    /// <returns>The alert severity, or <see langword="null"/> when the heart rate is within the baseline.</returns>
    /// <exception cref="ArgumentException">Thrown when the heart rate or the baseline is invalid.</exception>
    public static AlertSeverity? HeartRateSeverityFor(int heartRate, int baselineMin, int baselineMax)
    {
        if (heartRate <= 0)
            throw new ArgumentException("Heart rate must be greater than zero.", nameof(heartRate));
        if (baselineMin <= 0 || baselineMax <= baselineMin)
            throw new ArgumentException("The baseline requires a positive minimum lower than its maximum.",
                nameof(baselineMin));
        return heartRate < baselineMin || heartRate > baselineMax ? AlertSeverity.High : null;
    }

    /// <summary>
    ///     Evaluates a simulated route deviation against the demonstration threshold.
    /// </summary>
    /// <param name="deviationMeters">The simulated deviation from the route, in meters.</param>
    /// <returns>The alert severity, or <see langword="null"/> when the deviation does not exceed the threshold.</returns>
    /// <exception cref="ArgumentException">Thrown when the deviation is negative or not a finite number.</exception>
    public static AlertSeverity? RouteDeviationSeverityFor(double deviationMeters)
    {
        if (!double.IsFinite(deviationMeters) || deviationMeters < 0)
            throw new ArgumentException("Deviation must be a finite, non-negative distance.", nameof(deviationMeters));
        return deviationMeters > RouteDeviationThresholdMeters ? AlertSeverity.High : null;
    }
}
