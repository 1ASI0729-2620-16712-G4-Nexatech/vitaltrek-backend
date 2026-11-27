using System;
using Xunit;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

namespace VitalTrek.API.Tests.SafetyMonitoring.Domain;

public class EarlyWarningAlertTests
{
	private static readonly DateTimeOffset RaisedAt = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

	private static RaiseEarlyWarningAlertCommand Command(int? checkpointId = 3, string description = "Group delayed",
		AlertSeverity severity = AlertSeverity.High) =>
		new(1, 2, checkpointId, AlertType.Delay, severity, description);

	private static EarlyWarningAlert NewAlert() => new(Command(), 5, RaisedAt);

	[Fact]
	public void Constructor_WithValidData_IsActive()
	{
		var alert = NewAlert();
		Assert.Equal(AlertStatus.Active, alert.Status);
		Assert.Equal(5, alert.ExpeditionGroupId);
		Assert.Equal(3, alert.CheckpointId);
		Assert.Equal(RaisedAt, alert.RaisedAt);
		Assert.True(alert.IsOpen);
	}

	[Fact]
	public void Constructor_WithoutCheckpoint_IsAccepted()
	{
		var alert = new EarlyWarningAlert(Command(checkpointId: null), 5, RaisedAt);
		Assert.Null(alert.CheckpointId);
	}

	[Fact]
	public void Constructor_WithEmptyDescription_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => new EarlyWarningAlert(Command(description: "  "), 5, RaisedAt));
	}

	[Fact]
	public void Constructor_WithInvalidGroup_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => new EarlyWarningAlert(Command(), 0, RaisedAt));
	}

	[Fact]
	public void Acknowledge_ThenResolve_IsResolved()
	{
		var alert = NewAlert();
		alert.Acknowledge();
		Assert.Equal(AlertStatus.Acknowledged, alert.Status);
		alert.Resolve();
		Assert.Equal(AlertStatus.Resolved, alert.Status);
		Assert.False(alert.IsOpen);
	}

	[Fact]
	public void Resolve_WhenActive_ThrowsInvalidOperationException()
	{
		Assert.Throws<InvalidOperationException>(() => NewAlert().Resolve());
	}

	[Fact]
	public void Dismiss_WhenAcknowledged_IsDismissed()
	{
		var alert = NewAlert();
		alert.Acknowledge();
		alert.Dismiss();
		Assert.Equal(AlertStatus.Dismissed, alert.Status);
	}

	[Fact]
	public void Dismiss_WhenResolved_ThrowsInvalidOperationException()
	{
		var alert = NewAlert();
		alert.Acknowledge();
		alert.Resolve();
		Assert.Throws<InvalidOperationException>(() => alert.Dismiss());
	}

	[Fact]
	public void Acknowledge_Twice_ThrowsInvalidOperationException()
	{
		var alert = NewAlert();
		alert.Acknowledge();
		Assert.Throws<InvalidOperationException>(() => alert.Acknowledge());
	}

	[Fact]
	public void IsCritical_WhenDismissed_IsFalse()
	{
		var alert = new EarlyWarningAlert(Command(severity: AlertSeverity.Critical), 5, RaisedAt);
		Assert.True(alert.IsCritical);
		alert.Dismiss();
		Assert.False(alert.IsCritical);
	}

	[Theory]
	[InlineData(60, null)]
	[InlineData(61, AlertSeverity.High)]
	[InlineData(119, AlertSeverity.High)]
	[InlineData(120, AlertSeverity.Critical)]
	public void DelaySeverityFor_ReturnsExpectedSeverity(int elapsedMinutes, AlertSeverity? expected)
	{
		Assert.Equal(expected, EarlyWarningAlert.DelaySeverityFor(TimeSpan.FromMinutes(elapsedMinutes), 60));
	}

	[Theory]
	[InlineData("vital_sign_anomaly", AlertType.VitalSignAnomaly)]
	[InlineData("DELAY", AlertType.Delay)]
	public void AlertTypeFromCode_ParsesKnownCodes(string code, AlertType expected)
	{
		Assert.Equal(expected, AlertTypeExtensions.FromCode(code));
	}

	[Fact]
	public void AlertTypeFromCode_WithUnknownCode_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => AlertTypeExtensions.FromCode("flood"));
	}

	private static readonly DateTimeOffset CapturedAt = new(2026, 11, 10, 6, 40, 0, TimeSpan.FromHours(-5));
	private static readonly Guid ReadingId = Guid.Parse("7f1c2a4e-5b6d-4e8f-9a0b-1c2d3e4f5a6b");

	private static AlertEvidence HeartRateEvidence() =>
		new(160, 50, 140, AlertEvidence.BeatsPerMinute, CapturedAt, ReadingId);

	private static RecordEvaluatedAlertCommand Evaluated(SafetyRule rule = SafetyRule.HeartRateOutsideBaseline,
		int? checkpointId = null, int? memberId = 60, AlertEvidence? evidence = null) =>
		new(1, 2, checkpointId, memberId, rule, AlertSeverity.High, evidence ?? HeartRateEvidence(),
			ReadingId.ToString(), "Heart rate outside the demonstration baseline.");

	[Fact]
	public void EvaluatedAlert_KeepsRuleEvidenceAndOriginalCaptureTime()
	{
		var alert = new EarlyWarningAlert(Evaluated(), 5, RaisedAt);

		Assert.Equal(SafetyRule.HeartRateOutsideBaseline, alert.Rule);
		Assert.Equal(AlertType.VitalSignAnomaly, alert.AlertType);
		Assert.Equal(AlertStatus.Active, alert.Status);
		Assert.Equal(60, alert.MemberId);
		Assert.Equal($"heart_rate_outside_baseline:{ReadingId}", alert.EvidenceKey);
		Assert.Equal(HeartRateEvidence(), alert.Evidence);
		Assert.Equal(CapturedAt, alert.Evidence!.RecordedAt);
		Assert.Equal(TimeSpan.Zero, alert.Evidence.RecordedAt.Offset);
		Assert.Equal(RaisedAt, alert.RaisedAt);
		Assert.False(alert.Evidence.Simulated);
	}

	[Fact]
	public void EvaluatedDeviationAndDelayAlerts_RaiseTheirAlertTypes()
	{
		var deviation = new EarlyWarningAlert(Evaluated(SafetyRule.SimulatedRouteDeviation,
			evidence: new AlertEvidence(150, null, 100, AlertEvidence.Meters, CapturedAt, ReadingId, simulated: true)), 5, RaisedAt);
		var delay = new EarlyWarningAlert(Evaluated(SafetyRule.CheckpointDelay, checkpointId: 3, memberId: null,
			evidence: new AlertEvidence(95, null, 60, AlertEvidence.Minutes, CapturedAt)), 5, RaisedAt);

		Assert.Equal(AlertType.RouteDeviation, deviation.AlertType);
		Assert.True(deviation.Evidence!.Simulated);
		Assert.Equal(AlertType.Delay, delay.AlertType);
		Assert.Null(delay.MemberId);
	}

	[Fact]
	public void ManualAlert_HasManualRuleAndADistinctEvidenceIdentity()
	{
		var first = NewAlert();
		var second = NewAlert();

		Assert.Equal(SafetyRule.Manual, first.Rule);
		Assert.StartsWith("manual:", first.EvidenceKey);
		Assert.NotEqual(first.EvidenceKey, second.EvidenceKey);
		Assert.Null(first.Evidence);
	}

	[Fact]
	public void EvaluatedAlert_WithManualRule_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => new EarlyWarningAlert(Evaluated(SafetyRule.Manual), 5, RaisedAt));
	}

	[Fact]
	public void HeartRateAlert_WithoutParticipant_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => new EarlyWarningAlert(Evaluated(memberId: null), 5, RaisedAt));
	}

	[Fact]
	public void DeviationAlert_WithoutSimulatedEvidence_ThrowsArgumentException()
	{
		var evidence = new AlertEvidence(150, null, 100, AlertEvidence.Meters, CapturedAt, ReadingId);
		Assert.Throws<ArgumentException>(() =>
			new EarlyWarningAlert(Evaluated(SafetyRule.SimulatedRouteDeviation, evidence: evidence), 5, RaisedAt));
	}

	[Fact]
	public void DelayAlert_ForAParticipantOrWithoutCheckpoint_ThrowsArgumentException()
	{
		var evidence = new AlertEvidence(95, null, 60, AlertEvidence.Minutes, CapturedAt);
		Assert.Throws<ArgumentException>(() =>
			new EarlyWarningAlert(Evaluated(SafetyRule.CheckpointDelay, checkpointId: 3, evidence: evidence), 5, RaisedAt));
		Assert.Throws<ArgumentException>(() =>
			new EarlyWarningAlert(Evaluated(SafetyRule.CheckpointDelay, memberId: null, evidence: evidence), 5, RaisedAt));
	}

	[Theory]
	[InlineData(null, null, AlertEvidence.BeatsPerMinute)]
	[InlineData(140, 50, AlertEvidence.BeatsPerMinute)]
	[InlineData(50, 140, "percent")]
	public void Evidence_WithInvalidLimitsOrUnit_ThrowsArgumentException(int? minimum, int? maximum, string unit)
	{
		Assert.Throws<ArgumentException>(() => new AlertEvidence(160, minimum, maximum, unit, CapturedAt));
	}

	[Theory]
	[InlineData("heart_rate_outside_baseline", SafetyRule.HeartRateOutsideBaseline)]
	[InlineData("SIMULATED_ROUTE_DEVIATION", SafetyRule.SimulatedRouteDeviation)]
	[InlineData("checkpoint_delay", SafetyRule.CheckpointDelay)]
	[InlineData("manual", SafetyRule.Manual)]
	public void SafetyRuleFromCode_ParsesKnownCodes(string code, SafetyRule expected)
	{
		Assert.Equal(expected, SafetyRuleExtensions.FromCode(code));
		Assert.Equal(code.ToLowerInvariant(), expected.ToCode());
	}

	[Fact]
	public void ManualRule_DoesNotDetermineAnAlertType()
	{
		Assert.Throws<ArgumentException>(() => SafetyRule.Manual.ToAlertType());
	}
}
