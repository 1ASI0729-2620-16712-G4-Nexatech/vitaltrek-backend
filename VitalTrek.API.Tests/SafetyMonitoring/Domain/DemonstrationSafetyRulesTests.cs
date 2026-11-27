using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Services;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring.Domain;

/// <summary>
///     D T1 and D T2: demonstration thresholds of the academic simulation. These values are not a medical recommendation.
/// </summary>
public class DemonstrationSafetyRulesTests
{
    private const int BaselineMin = 50, BaselineMax = 140;

    [Theory]
    [InlineData(80)]
    [InlineData(50)]
    [InlineData(140)]
    public void HeartRateWithinTheBaselineIncludingItsLimitsDoesNotRaise(int heartRate)
    {
        Assert.Null(DemonstrationSafetyRules.HeartRateSeverityFor(heartRate, BaselineMin, BaselineMax));
    }

    [Theory]
    [InlineData(160)]
    [InlineData(49)]
    [InlineData(141)]
    public void HeartRateOutsideTheBaselineRaisesAHighAlert(int heartRate)
    {
        Assert.Equal(AlertSeverity.High, DemonstrationSafetyRules.HeartRateSeverityFor(heartRate, BaselineMin, BaselineMax));
    }

    [Theory]
    [InlineData(0, 50, 140)]
    [InlineData(80, 0, 140)]
    [InlineData(80, 140, 140)]
    [InlineData(80, 140, 50)]
    public void InvalidHeartRateOrBaselineIsRejected(int heartRate, int baselineMin, int baselineMax)
    {
        Assert.Throws<ArgumentException>(() =>
            DemonstrationSafetyRules.HeartRateSeverityFor(heartRate, baselineMin, baselineMax));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(100d)]
    public void DeviationUpToTheThresholdDoesNotRaise(double deviationMeters)
    {
        Assert.Null(DemonstrationSafetyRules.RouteDeviationSeverityFor(deviationMeters));
    }

    [Theory]
    [InlineData(100.5d)]
    [InlineData(101d)]
    public void DeviationAboveTheThresholdRaisesAHighAlert(double deviationMeters)
    {
        Assert.Equal(AlertSeverity.High, DemonstrationSafetyRules.RouteDeviationSeverityFor(deviationMeters));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidDeviationIsRejected(double deviationMeters)
    {
        Assert.Throws<ArgumentException>(() => DemonstrationSafetyRules.RouteDeviationSeverityFor(deviationMeters));
    }
}
