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
}
