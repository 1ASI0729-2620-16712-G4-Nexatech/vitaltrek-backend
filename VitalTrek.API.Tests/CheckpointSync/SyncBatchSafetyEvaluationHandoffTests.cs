using System.Net;
using System.Text.Json;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.CheckpointSync;

/// <summary>
///     D01 hand-off: right after a batch is committed, Checkpoint Sync asks Safety Monitoring to evaluate it through the
///     internal port. A failed evaluation never changes the 202 reception result; the batch stays pending for the
///     periodic evaluation, and a replay of an evaluated batch is not evaluated again.
/// </summary>
public sealed class SyncBatchSafetyEvaluationHandoffTests
{
    private const string Path = "/api/v1/sync-batches";
    private const int Guide = 2;
    // Whole seconds, like the other sync batch tests.
    private static readonly DateTimeOffset CapturedAt =
        DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds());

    private readonly List<(int AgencyId, int BatchId)> _evaluations = [];

    /// <summary>Simulated outcome of the Safety Monitoring evaluation.</summary>
    public enum Outcome
    {
        Evaluated,
        Pending,
        Throws
    }

    [Fact]
    public Task AcceptedBatchIsEvaluatedRightAfterItsCommit() => WithApi(Outcome.Evaluated, async (data, api) =>
    {
        using var response = await api.Send("POST", Path, Guide, Batch());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await Json(response);
        Assert.Equal("evaluated", body.GetProperty("evaluationStatus").GetString());
        Assert.Equal((data.Session.AgencyId, body.GetProperty("batchId").GetInt32()), Assert.Single(_evaluations));
        Assert.Equal(1, data.Saves);
    });

    [Theory]
    [InlineData(Outcome.Pending)]
    [InlineData(Outcome.Throws)]
    public Task FailedEvaluationStillAnswers202WithThePendingBatch(Outcome outcome) => WithApi(outcome, async (data, api) =>
    {
        using var response = await api.Send("POST", Path, Guide, Batch());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("pending", (await Json(response)).GetProperty("evaluationStatus").GetString());
        Assert.Single(_evaluations);
        Assert.Single(data.StoredBatches);
    });

    [Fact]
    public Task ReplayOfAnEvaluatedBatchIsNotEvaluatedAgain() => WithApi(Outcome.Evaluated, async (_, api) =>
    {
        var batch = Batch();

        using var first = await api.Send("POST", Path, Guide, batch);
        using var replay = await api.Send("POST", Path, Guide, batch);

        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
        Assert.Equal("evaluated", (await Json(replay)).GetProperty("evaluationStatus").GetString());
        Assert.Single(_evaluations);
    });

    private async Task WithApi(Outcome outcome, Func<SyncBatchData, SyncBatchApi, Task> assertion)
    {
        var data = await SyncBatchData.CreateAsync();
        var safetyMonitoring = InterfaceStub<ISafetyMonitoringContextFacade>.Create((method, args) =>
        {
            if (method.Name != nameof(ISafetyMonitoringContextFacade.EvaluateSyncBatchAsync))
                throw new InvalidOperationException(method.Name);
            var (agencyId, batchId) = ((int)args[0]!, (int)args[1]!);
            _evaluations.Add((agencyId, batchId));
            return outcome switch
            {
                Outcome.Evaluated => MarkEvaluated(data, agencyId, batchId),
                Outcome.Pending => Task.FromResult(false),
                _ => Task.FromException<bool>(new InvalidOperationException("Simulated evaluation failure."))
            };
        });
        await using var api = await SyncBatchApi.StartAsync(data, safetyMonitoring);
        await assertion(data, api);
    }

    // Like the real evaluation, the committed batch is marked as evaluated within the same request scope.
    private static Task<bool> MarkEvaluated(SyncBatchData data, int agencyId, int batchId)
    {
        data.StoredBatches.Single(batch => batch.Id == batchId && batch.AgencyId == agencyId).MarkEvaluated();
        return Task.FromResult(true);
    }

    private static object Batch() => new
    {
        clientBatchId = Guid.NewGuid(),
        sessionId = 501,
        checkpointId = 30,
        readings = new object[]
        {
            new
            {
                clientRecordId = Guid.NewGuid(), wearableDeviceId = 70, recordedAt = CapturedAt, latitude = -9.5,
                longitude = -77.5, altitude = 3000, heartRate = 88, battery = 85, deviationMeters = 12.5
            }
        },
        passages = new object[]
        {
            new { clientRecordId = Guid.NewGuid(), checkpointId = 30, passedAt = CapturedAt.AddMinutes(1) }
        }
    };

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
