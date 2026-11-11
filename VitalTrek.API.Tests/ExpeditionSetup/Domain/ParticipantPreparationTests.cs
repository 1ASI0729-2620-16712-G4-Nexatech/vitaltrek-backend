using System;
using Xunit;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.Tests.ExpeditionSetup.Domain;

public class ParticipantPreparationTests
{
	private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

	[Fact]
	public void GrantConsent_WithoutWearableDevice_ThrowsException()
	{
		var preparation = new ParticipantPreparation();
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		Assert.ThrowsAny<Exception>(() => preparation.GrantConsent(Now));
	}

	[Fact]
	public void GrantConsent_WithoutEmergencyContact_ThrowsException()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearable(1);
		Assert.ThrowsAny<Exception>(() => preparation.GrantConsent(Now));
	}

	[Fact]
	public void GrantConsent_WithWearableAndContact_SetsConsentAndActivated()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearable(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent(Now);
		Assert.True(preparation.Consent);
		Assert.True(preparation.Activated);
	}

	[Fact]
	public void RevokeConsent_SetsConsentAndActivatedToFalse()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearable(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent(Now);
		preparation.RevokeConsent(Now.AddMinutes(1));
		Assert.False(preparation.Consent);
		Assert.False(preparation.Activated);
	}

	[Fact]
	public void AssignWearable_DifferentDevice_DeactivatesPrevious()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearable(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent(Now);
		preparation.AssignWearable(2);
		Assert.False(preparation.Activated);
	}

	[Fact]
	public void IsReady_WithWearableConsentActivationAndContact_ReturnsTrue()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearable(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent(Now);
		Assert.True(preparation.IsReady);
	}
}
