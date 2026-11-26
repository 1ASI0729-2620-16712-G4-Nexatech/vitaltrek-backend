using System.Net;
using System.Text.Json;
using VitalTrek.API.CheckpointSync.Application.ACL;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.CheckpointSync;

/// <summary>Delivery point for Safety Monitoring (D01 and D02): accepted readings, counts and evaluation status.</summary>
public sealed class SyncBatchContextFacadeTests
{
    private static readonly DateTimeOffset CapturedAt =
        DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds());

    [Fact]
    public async Task BatchForEvaluationExposesAcceptedReadingsWithCaptureTimeCountsAndStatus()
    {
        var data = await SyncBatchData.CreateAsync();
        var (reading, passage, rejected) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var batchId = await Submit(data, [Reading(reading, 160), Reading(rejected, 0)], [Passage(passage)]);
        var facade = Facade(data);

        var input = await facade.FindBatchForEvaluationAsync(1, batchId);

        Assert.NotNull(input);
        Assert.Equal(501, input.SessionId);
        Assert.Equal("pending", input.EvaluationStatus);
        Assert.Equal((2, 0, 1), (input.AcceptedCount, input.DuplicateCount, input.RejectedCount));
        var accepted = Assert.Single(input.AcceptedReadings);
        Assert.Equal(reading, accepted.ClientRecordId);
        Assert.Equal(60, accepted.MemberId);
        Assert.Equal(160, accepted.HeartRate);
        Assert.Equal(CapturedAt, accepted.RecordedAt);
        Assert.True(accepted.SynchronizedAt > accepted.RecordedAt);
        Assert.Equal([passage], input.AcceptedPassageIds);
        Assert.Null(await facade.FindBatchForEvaluationAsync(2, batchId));
    }

    [Fact]
    public async Task PendingBatchesCanBeRecoveredAndAreMarkedEvaluatedOnlyByAnExplicitStage()
    {
        var data = await SyncBatchData.CreateAsync();
        var batchId = await Submit(data, [Reading(Guid.NewGuid(), 88)], [Passage(Guid.NewGuid())]);
        var facade = Facade(data);

        var pending = Assert.Single(await facade.FindPendingEvaluationsAsync(10));
        Assert.Equal((batchId, 1, 501), (pending.BatchId, pending.AgencyId, pending.SessionId));
        Assert.False(await facade.StageEvaluationCompletedAsync(2, batchId));
        Assert.True(await facade.StageEvaluationCompletedAsync(1, batchId));
        Assert.True(await facade.StageEvaluationCompletedAsync(1, batchId));

        Assert.Equal(SyncBatchEvaluationStatus.Evaluated, Assert.Single(data.StoredBatches).EvaluationStatus);
        Assert.Empty(await facade.FindPendingEvaluationsAsync(10));
        Assert.Equal("evaluated", (await facade.FindBatchForEvaluationAsync(1, batchId))!.EvaluationStatus);
    }

    private static SyncBatchContextFacade Facade(SyncBatchData data) => new(data.BatchRepository,
        new FieldTrackingContextFacade(data.Sessions, data.Ready.Expeditions,
            InterfaceStub<ITelemetryRecordCommandService>.Create(), data.TelemetryRepository));

    private static async Task<int> Submit(SyncBatchData data, object[] readings, object[] passages)
    {
        await using var api = await SyncBatchApi.StartAsync(data);
        using var response = await api.Send("POST", "/api/v1/sync-batches", 2, new
        {
            clientBatchId = Guid.NewGuid(), sessionId = 501, checkpointId = 30, readings, passages
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("batchId").GetInt32();
    }

    private static object Reading(Guid id, int heartRate) => new
    {
        clientRecordId = id, wearableDeviceId = 70, recordedAt = CapturedAt, latitude = -9.5, longitude = -77.5,
        altitude = 3000, heartRate, battery = 85, deviationMeters = 12.5
    };

    private static object Passage(Guid id) => new { clientRecordId = id, checkpointId = 30, passedAt = CapturedAt.AddMinutes(1) };
}
