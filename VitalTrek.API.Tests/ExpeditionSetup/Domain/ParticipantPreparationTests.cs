using System;
using Xunit;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.Tests.ExpeditionSetup.Domain;

public class ParticipantPreparationTests
{
	[Fact]
	public void GrantConsent_WithoutWearableDevice_ThrowsException()
	{
		var preparation = new ParticipantPreparation();
		var contact = new EmergencyContact("Test Name", "999999999");
		preparation.RegisterEmergencyContact(contact);
		Assert.ThrowsAny<Exception>(() => preparation.GrantConsent());
	}

	[Fact]
	public void GrantConsent_WithoutEmergencyContact_ThrowsException()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearableDevice(1);
		Assert.ThrowsAny<Exception>(() => preparation.GrantConsent());
	}

	[Fact]
	public void GrantConsent_WithWearableAndContact_SetsConsentAndActivated()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearableDevice(1);
		var contact = new EmergencyContact("Test Name", "999999999");
		preparation.RegisterEmergencyContact(contact);
		preparation.GrantConsent();
		Assert.True(preparation.Consent);
		Assert.True(preparation.Activated);
	}

	[Fact]
	public void RevokeConsent_SetsConsentAndActivatedToFalse()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearableDevice(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent();
		preparation.RevokeConsent();
		Assert.False(preparation.Consent);
		Assert.False(preparation.Activated);
	}

	[Fact]
	public void AssignWearableDevice_DifferentDevice_DeactivatesPrevious()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearableDevice(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent();
		preparation.AssignWearableDevice(2);
		Assert.False(preparation.Activated);
	}

	[Fact]
	public void IsReady_WithWearableConsentActivationAndContact_ReturnsTrue()
	{
		var preparation = new ParticipantPreparation();
		preparation.AssignWearableDevice(1);
		preparation.RegisterEmergencyContact(new EmergencyContact("Test Name", "999999999"));
		preparation.GrantConsent();
		Assert.True(preparation.IsReady);
	}
}