using System;
using Xunit;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.Tests.ExpeditionSetup.Domain;

public class ExpeditionGroupTests
{
	private static readonly DateOnly Today = new(2026, 11, 10);

	private static ExpeditionGroup CreateGroup(int capacity = 10, DateOnly? departure = null) =>
		new(new CreateExpeditionGroupCommand(1, 1, "Group A", departure ?? Today.AddDays(1), capacity), Today);

	[Fact]
	public void Constructor_NewGroup_IsScheduledIsTrue()
	{
		var group = CreateGroup();
		Assert.True(group.IsScheduled);
	}

	[Fact]
	public void AssignFieldGuide_ValidIdentifier_SavesIdentifier()
	{
		var group = CreateGroup();
		group.AssignFieldGuide(42);
		Assert.Equal(42, group.FieldGuideId);
	}

	[Fact]
	public void Start_FromScheduledStateWithGuide_ChangesStatusToActive()
	{
		var group = CreateGroup();
		group.AssignFieldGuide(42);
		group.Start(DateTimeOffset.UtcNow);
		Assert.Equal(ExpeditionGroupStatus.Active, group.Status);
	}

	[Fact]
	public void Start_WithoutFieldGuide_ThrowsException()
	{
		var group = CreateGroup();
		Assert.Throws<InvalidOperationException>(() => group.Start(DateTimeOffset.UtcNow));
	}

	[Fact]
	public void Finish_WhenGroupIsNotActive_ThrowsException()
	{
		var group = CreateGroup();
		Assert.Throws<InvalidOperationException>(() => group.Finish(DateTimeOffset.UtcNow));
	}

	[Fact]
	public void Constructor_InvalidCapacity_ThrowsException()
	{
		Assert.Throws<ArgumentException>(() => CreateGroup(capacity: 0));
	}

	[Fact]
	public void Constructor_DepartureDateInPast_ThrowsException()
	{
		Assert.Throws<ArgumentException>(() => CreateGroup(departure: Today.AddDays(-1)));
	}
}
