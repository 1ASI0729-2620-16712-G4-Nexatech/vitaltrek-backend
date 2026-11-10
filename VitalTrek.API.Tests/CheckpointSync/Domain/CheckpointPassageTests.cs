using System;
using Xunit;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

namespace VitalTrek.API.Tests.CheckpointSync.Domain;

public class CheckpointPassageTests
{
	private static readonly DateTimeOffset SyncedAt = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

	private static RecordCheckpointPassageCommand Command(DateTimeOffset passedAt, int sessionId = 1) =>
		new(1, 2, sessionId, 3, passedAt);

	[Fact]
	public void Constructor_WithinOnTimeDelay_IsSynced()
	{
		var passage = new CheckpointPassage(Command(SyncedAt.AddMinutes(-30)), SyncedAt);
		Assert.Equal(CheckpointPassageStatus.Synced, passage.Status);
		Assert.Equal(SyncedAt, passage.SyncedAt);
	}

	[Fact]
	public void Constructor_BeyondOnTimeDelay_IsDelayedSync()
	{
		var passage = new CheckpointPassage(Command(SyncedAt.AddMinutes(-31)), SyncedAt);
		Assert.Equal(CheckpointPassageStatus.DelayedSync, passage.Status);
	}

	[Fact]
	public void Constructor_PassedAtWithinClockTolerance_IsAccepted()
	{
		var passage = new CheckpointPassage(Command(SyncedAt.AddSeconds(30)), SyncedAt);
		Assert.Equal(CheckpointPassageStatus.Synced, passage.Status);
	}

	[Fact]
	public void Constructor_PassedAtBeyondClockTolerance_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => new CheckpointPassage(Command(SyncedAt.AddMinutes(2)), SyncedAt));
	}

	[Fact]
	public void Constructor_NonPositiveSessionId_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => new CheckpointPassage(Command(SyncedAt, sessionId: 0), SyncedAt));
	}

	[Fact]
	public void StatusCodes_RoundTrip()
	{
		Assert.Equal("delayedsync", CheckpointPassageStatus.DelayedSync.ToCode());
		Assert.Equal(CheckpointPassageStatus.Synced, CheckpointPassageStatusExtensions.FromCode("SYNCED"));
		Assert.Throws<ArgumentException>(() => CheckpointPassageStatusExtensions.FromCode("x"));
	}
}
