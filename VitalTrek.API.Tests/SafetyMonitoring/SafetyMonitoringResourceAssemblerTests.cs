using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;
using VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Transform;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring;

/// <summary>The alert resource exposes the rule code and the evidence with its original capture time.</summary>
public class SafetyMonitoringResourceAssemblerTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 11, 26, 9, 30, 0, TimeSpan.FromHours(-5));
    private static readonly DateTimeOffset RaisedAt = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EvaluatedAlertExposesItsRuleParticipantAndEvidence()
    {
        var readingId = Guid.NewGuid();
        var alert = new EarlyWarningAlert(new RecordEvaluatedAlertCommand(1, 7, null, 60,
            SafetyRule.SimulatedRouteDeviation, AlertSeverity.High,
            new AlertEvidence(150, null, 100, AlertEvidence.Meters, CapturedAt, readingId, simulated: true),
            readingId.ToString(), "Simulated route deviation."), 5, RaisedAt);

        var resource = SafetyMonitoringResourceAssembler.ToResourceFromEntity(alert);

        Assert.Equal("route_deviation", resource.AlertType);
        Assert.Equal("simulated_route_deviation", resource.Rule);
        Assert.Equal(60, resource.MemberId);
        Assert.Equal(RaisedAt, resource.RaisedAt);
        Assert.Equal(new AlertEvidenceResource(150, null, 100, "m", CapturedAt, readingId, true), resource.Evidence);
    }

    [Fact]
    public void ManualAlertHasTheManualRuleAndNoEvidence()
    {
        var alert = new EarlyWarningAlert(new RaiseEarlyWarningAlertCommand(1, 7, 3, AlertType.Other, AlertSeverity.Medium,
            "Reported by the guide."), 5, RaisedAt);

        var resource = SafetyMonitoringResourceAssembler.ToResourceFromEntity(alert);

        Assert.Equal("manual", resource.Rule);
        Assert.Null(resource.MemberId);
        Assert.Null(resource.Evidence);
    }
}
