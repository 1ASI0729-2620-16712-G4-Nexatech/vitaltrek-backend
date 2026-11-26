using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.CheckpointSync.Domain.Services;
using Xunit;

namespace VitalTrek.API.Tests.CheckpointSync.Domain;

public class SyncBatchTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 11, 26, 10, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void NewBatchIsPendingEvaluationAndStoresTheReceptionInUtc()
    {
        var batch = new SyncBatch(Command(), SyncBatchFingerprint.Compute(Command()), CapturedAt.AddMinutes(30));

        Assert.Equal(SyncBatchEvaluationStatus.Pending, batch.EvaluationStatus);
        Assert.Equal(TimeSpan.Zero, batch.ReceivedAt.Offset);
        Assert.Equal(CapturedAt.AddMinutes(30), batch.ReceivedAt);
        Assert.Empty(batch.Records);
    }

    [Fact]
    public void RecordOutcomesAreListedInAStableOrderAndRejectionsKeepTheirCode()
    {
        var batch = new SyncBatch(Command(), SyncBatchFingerprint.Compute(Command()), CapturedAt);
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        batch.Accept(ids[0], SyncRecordKind.Reading);
        batch.Accept(ids[1], SyncRecordKind.Passage);
        batch.MarkDuplicate(ids[2], SyncRecordKind.Reading);
        batch.Reject(ids[3], SyncRecordKind.Reading, "Telemetry.InvalidData");

        Assert.Equal(new[] { ids[0], ids[1] }.Order(), batch.RecordIdsWith(SyncRecordOutcome.Accepted));
        Assert.Equal([ids[2]], batch.RecordIdsWith(SyncRecordOutcome.Duplicate));
        var rejected = Assert.Single(batch.RejectedRecords());
        Assert.Equal(ids[3], rejected.ClientRecordId);
        Assert.Equal("Telemetry.InvalidData", rejected.RejectionCode);
    }

    [Fact]
    public void ARecordCannotHaveTwoOutcomesAndARejectionNeedsACode()
    {
        var batch = new SyncBatch(Command(), SyncBatchFingerprint.Compute(Command()), CapturedAt);
        var id = Guid.NewGuid();
        batch.Accept(id, SyncRecordKind.Reading);

        Assert.Throws<InvalidOperationException>(() => batch.MarkDuplicate(id, SyncRecordKind.Reading));
        Assert.Throws<ArgumentException>(() => batch.Reject(Guid.NewGuid(), SyncRecordKind.Reading, " "));
        Assert.Throws<ArgumentException>(() => batch.Accept(Guid.Empty, SyncRecordKind.Reading));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("session")]
    [InlineData("fingerprint")]
    public void InvalidBatchDataIsRejected(string fault)
    {
        var command = fault switch
        {
            "identity" => Command() with { ClientBatchId = Guid.Empty },
            "session" => Command() with { SessionId = 0 },
            _ => Command()
        };
        var fingerprint = fault == "fingerprint" ? "not-a-hash" : SyncBatchFingerprint.Compute(Command());

        Assert.Throws<ArgumentException>(() => new SyncBatch(command, fingerprint, CapturedAt));
    }

    [Fact]
    public void FingerprintIgnoresRecordOrderOffsetsAndServerResolvedIdentities()
    {
        var command = Command();
        var reordered = command with
        {
            AgencyId = 99, FieldGuideId = 98,
            Readings = command.Readings.Reverse().Select(reading => reading with
            {
                RecordedAt = reading.RecordedAt.ToOffset(TimeSpan.Zero)
            }).ToArray()
        };

        Assert.Equal(SyncBatchFingerprint.Compute(command), SyncBatchFingerprint.Compute(reordered));
    }

    [Theory]
    [InlineData("heartRate")]
    [InlineData("recordedAt")]
    [InlineData("passage")]
    [InlineData("checkpoint")]
    public void FingerprintChangesWhenTheCapturedContentChanges(string change)
    {
        var command = Command();
        var changed = change switch
        {
            "heartRate" => command with { Readings = [command.Readings[0] with { HeartRate = 99 }, command.Readings[1]] },
            "recordedAt" => command with
            {
                Readings = [command.Readings[0] with { RecordedAt = CapturedAt.AddSeconds(1) }, command.Readings[1]]
            },
            "passage" => command with { Passages = [command.Passages[0] with { CheckpointId = 31 }] },
            _ => command with { CheckpointId = 31 }
        };

        Assert.NotEqual(SyncBatchFingerprint.Compute(command), SyncBatchFingerprint.Compute(changed));
    }

    private static readonly Guid BatchId = Guid.Parse("8f0b8f2e-3f3e-4c1e-9b8a-000000000001");
    private static readonly Guid FirstReading = Guid.Parse("8f0b8f2e-3f3e-4c1e-9b8a-000000000002");
    private static readonly Guid SecondReading = Guid.Parse("8f0b8f2e-3f3e-4c1e-9b8a-000000000003");
    private static readonly Guid Passage = Guid.Parse("8f0b8f2e-3f3e-4c1e-9b8a-000000000004");

    private static SubmitSyncBatchCommand Command() => new(1, 10, BatchId, 501, 30,
        [
            new SyncBatchReadingEntry(FirstReading, 70, CapturedAt, -9.5, -77.5, 3000, 88, 85, 12.5),
            new SyncBatchReadingEntry(SecondReading, 71, CapturedAt.AddMinutes(1), -9.49, -77.49, 3010, 90, 84, 0)
        ],
        [new SyncBatchPassageEntry(Passage, 30, CapturedAt.AddMinutes(2))]);
}
