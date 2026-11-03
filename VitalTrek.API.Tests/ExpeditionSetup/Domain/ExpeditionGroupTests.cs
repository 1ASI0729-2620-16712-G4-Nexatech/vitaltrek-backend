using System;
using Xunit;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

namespace VitalTrek.API.Tests.ExpeditionSetup.Domain;

public class ExpeditionGroupTests
{
	[Fact]
	public void Constructor_NewGroup_IsScheduledIsTrue()
	{
		var command = new CreateExpeditionGroupCommand(1, 1, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));
		var group = new ExpeditionGroup(command);
		Assert.True(group.IsScheduled);
	}

	[Fact]
	public void AssignFieldGuide_ValidIdentifier_SavesIdentifier()
	{
		var command = new CreateExpeditionGroupCommand(1, 1, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));
		var group = new ExpeditionGroup(command);
		long expectedGuideId = 42;
		group.AssignFieldGuide(expectedGuideId);
		Assert.Equal(expectedGuideId, group.FieldGuideId);
	}

	[Fact]
	public void Start_FromScheduledState_ChangesStatusToActive()
	{
		var command = new CreateExpeditionGroupCommand(1, 1, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));
		var group = new ExpeditionGroup(command);
		group.Start();
		Assert.True(group.IsActive);
	}

	[Fact]
	public void End_WhenGroupIsNotActive_ThrowsException()
	{
		var command = new CreateExpeditionGroupCommand(1, 1, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));
		var group = new ExpeditionGroup(command);
		Assert.ThrowsAny<Exception>(() => group.End());
	}

	[Fact]
	public void Constructor_InvalidCapacity_ThrowsException()
	{
		var invalidCapacityCommand = new CreateExpeditionGroupCommand(1, 1, 0, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));
		Assert.ThrowsAny<Exception>(() => new ExpeditionGroup(invalidCapacityCommand));
	}
}