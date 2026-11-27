using System;
using Xunit;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

namespace VitalTrek.API.Tests.SafetyMonitoring.Domain;

public class EarlyWarningAlertTests
{
	private static readonly DateTimeOffset RaisedAt = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset ActedAt = new(2026, 11, 10, 7, 15, 0, TimeSpan.FromHours(-5));
	private const int UserId = 9;

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
		alert.Acknowledge(UserId, ActedAt);
		Assert.Equal(AlertStatus.Acknowledged, alert.Status);
		alert.Resolve(UserId, ActedAt);
		Assert.Equal(AlertStatus.Resolved, alert.Status);
		Assert.False(alert.IsOpen);
	}

	[Fact]
	public void Resolve_WhenActive_ThrowsInvalidOperationException()
	{
		Assert.Throws<InvalidOperationException>(() => NewAlert().Resolve(UserId, ActedAt));
	}

	[Fact]
	public void Dismiss_WhenAcknowledged_IsDismissed()
	{
		var alert = NewAlert();
		alert.Acknowledge(UserId, ActedAt);
		alert.Dismiss(UserId, ActedAt);
		Assert.Equal(AlertStatus.Dismissed, alert.Status);
	}

	[Fact]
	public void Dismiss_WhenResolved_ThrowsInvalidOperationException()
	{
		var alert = NewAlert();
		alert.Acknowledge(UserId, ActedAt);
		alert.Resolve(UserId, ActedAt);
		Assert.Throws<InvalidOperationException>(() => alert.Dismiss(UserId, ActedAt));
	}

	[Fact]
	public void Acknowledge_Twice_ThrowsInvalidOperationException()
	{
		var alert = NewAlert();
		alert.Acknowledge(UserId, ActedAt);
		Assert.Throws<InvalidOperationException>(() => alert.Acknowledge(UserId, ActedAt));
	}

	[Fact]
	public void IsCritical_WhenDismissed_IsFalse()
	{
		var alert = new EarlyWarningAlert(Command(severity: AlertSeverity.Critical), 5, RaisedAt);
		Assert.True(alert.IsCritical);
		alert.Dismiss(UserId, ActedAt);
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

	private static EarlyWarningAlert DelayAlert(AlertSeverity severity = AlertSeverity.High) =>
		new(new RecordEvaluatedAlertCommand(1, 2, 3, null, SafetyRule.CheckpointDelay, severity,
			new AlertEvidence(70, null, 60, AlertEvidence.Minutes, CapturedAt), "3", "Delayed by 70 minutes."), 5, RaisedAt);

	private static AlertEvidence ElapsedEvidence(int minutes) => new(minutes, null, 60, AlertEvidence.Minutes, CapturedAt);

	[Fact]
	public void Acknowledge_RecordsTheAuthorAndTheMomentInUtc()
	{
		var alert = NewAlert();
		alert.Acknowledge(UserId, ActedAt);

		Assert.Equal(UserId, alert.AcknowledgedByUserId);
		Assert.Equal(ActedAt, alert.AcknowledgedAt);
		Assert.Equal(TimeSpan.Zero, alert.AcknowledgedAt!.Value.Offset);
		Assert.Null(alert.ClosedByUserId);
	}

	[Fact]
	public void ResolveAndDismiss_RecordWhoClosedTheAlert()
	{
		var resolved = NewAlert();
		resolved.Acknowledge(UserId, ActedAt);
		resolved.Resolve(10, ActedAt.AddMinutes(20));
		var dismissed = NewAlert();
		dismissed.Dismiss(11, ActedAt.AddMinutes(5));

		Assert.Equal((UserId, 10), (resolved.AcknowledgedByUserId!.Value, resolved.ClosedByUserId!.Value));
		Assert.Equal(ActedAt.AddMinutes(20), resolved.ClosedAt);
		Assert.Equal(11, dismissed.ClosedByUserId);
		Assert.Null(dismissed.AcknowledgedByUserId);
	}

	[Fact]
	public void Transition_WithoutAValidUser_ThrowsArgumentExceptionAndKeepsTheStatus()
	{
		var alert = NewAlert();
		Assert.Throws<ArgumentException>(() => alert.Acknowledge(0, ActedAt));
		Assert.Throws<ArgumentException>(() => alert.Dismiss(-1, ActedAt));
		Assert.Equal(AlertStatus.Active, alert.Status);
	}

	[Fact]
	public void Escalate_WorseDelay_RaisesSeverityAndRefreshesEvidenceWithoutChangingTheStatus()
	{
		var alert = DelayAlert();
		alert.Acknowledge(UserId, ActedAt);

		Assert.True(alert.Escalate(AlertSeverity.Critical, ElapsedEvidence(130), "Delayed by 130 minutes."));
		Assert.Equal(AlertSeverity.Critical, alert.Severity);
		Assert.Equal(130m, alert.Evidence!.ObservedValue);
		Assert.Equal("Delayed by 130 minutes.", alert.Description);
		Assert.Equal(AlertStatus.Acknowledged, alert.Status);
	}

	[Theory]
	[InlineData(AlertSeverity.High)]
	[InlineData(AlertSeverity.Medium)]
	public void Escalate_SameOrLowerSeverity_LeavesTheAlertUnchanged(AlertSeverity severity)
	{
		var alert = DelayAlert();

		Assert.False(alert.Escalate(severity, ElapsedEvidence(90), "Delayed by 90 minutes."));
		Assert.Equal(AlertSeverity.High, alert.Severity);
		Assert.Equal(70m, alert.Evidence!.ObservedValue);
	}

	[Fact]
	public void Escalate_ClosedAlert_IsNeverReopenedOrChanged()
	{
		var alert = DelayAlert();
		alert.Dismiss(UserId, ActedAt);

		Assert.False(alert.Escalate(AlertSeverity.Critical, ElapsedEvidence(130), "Delayed by 130 minutes."));
		Assert.Equal(AlertStatus.Dismissed, alert.Status);
		Assert.Equal(AlertSeverity.High, alert.Severity);
	}

	[Fact]
	public void Escalate_AlertOfAnotherRule_ThrowsInvalidOperationException()
	{
		Assert.Throws<InvalidOperationException>(() =>
			new EarlyWarningAlert(Evaluated(), 5, RaisedAt).Escalate(AlertSeverity.Critical, ElapsedEvidence(130), "x"));
		Assert.Throws<InvalidOperationException>(() =>
			NewAlert().Escalate(AlertSeverity.Critical, ElapsedEvidence(130), "x"));
	}
}
