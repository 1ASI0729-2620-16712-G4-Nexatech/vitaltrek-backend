using System.Net;
using System.Text.Json;
using Xunit;

namespace VitalTrek.API.Tests.CheckpointSync;

/// <summary>S02 partial validation and identity reuse, and S03 passage rules, through the real HTTP pipeline.</summary>
public sealed class SyncBatchValidationHttpTests
{
    private const string Path = "/api/v1/sync-batches";
    private const string PassagePath = "/api/v1/checkpoint-passages";
    private const int Guide = 2, OtherGuide = 3;
    private const int First = 30, Second = 31;
    private static readonly DateTimeOffset CapturedAt =
        DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds());

    [Theory]
    [InlineData("heartRate", 0, "Telemetry.InvalidHeartRate")]
    [InlineData("heartRate", 301, "Telemetry.InvalidHeartRate")]
    [InlineData("battery", -1, "Telemetry.InvalidBattery")]
    [InlineData("battery", 101, "Telemetry.InvalidBattery")]
    [InlineData("deviationMeters", -1, "Telemetry.InvalidDeviation")]
    [InlineData("latitude", 91, "Telemetry.InvalidCoordinates")]
    [InlineData("longitude", -181, "Telemetry.InvalidCoordinates")]
    [InlineData("recordedAt", -120, "Telemetry.InvalidRecordedAt")]
    [InlineData("recordedAt", 30, "Telemetry.InvalidRecordedAt")]
    public Task EachInvalidFieldIsRejectedWithItsOwnCodeWithoutBlockingValidRows(string field, int value,
        string code) => WithApi(async (data, api) =>
    {
        var (valid, invalid) = (Guid.NewGuid(), Guid.NewGuid());
        var reading = Reading(invalid);
        reading[field] = field == "recordedAt" ? DateTimeOffset.UtcNow.AddMinutes(value) : value;

        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [Reading(valid), reading], []));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await Json(response);
        Assert.Equal([valid], Guids(body, "acceptedRecordIds"));
        var rejected = Assert.Single(body.GetProperty("rejectedRecords").EnumerateArray());
        Assert.Equal(invalid, rejected.GetProperty("clientRecordId").GetGuid());
        Assert.Equal(code, rejected.GetProperty("code").GetString());
        Assert.Equal(valid, Assert.Single(data.StoredReadings).ClientRecordId);
    });

    [Theory]
    [InlineData(49)]
    [InlineData(160)]
    public Task ClinicallyAnomalousPulseIsAcceptedForLaterEvaluation(int heartRate) => WithApi(async (data, api) =>
    {
        var reading = Reading(Guid.NewGuid());
        reading["heartRate"] = heartRate;

        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [reading], []));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(heartRate, Assert.Single(data.StoredReadings).HeartRate);
    });

    [Fact]
    public Task ReusedPassageIdentityWithDifferentDataReturns409() => WithApi(async (data, api) =>
    {
        var passageId = Guid.NewGuid();
        using var first = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [], [Passage(passageId, First)]));
        using var conflict = await api.Send("POST", Path, Guide,
            Batch(Guid.NewGuid(), [], [Passage(passageId, First, CapturedAt.AddMinutes(3))]));

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("SyncBatch.RecordIdentityConflict", (await Json(conflict)).GetProperty("code").GetString());
        Assert.Equal(CapturedAt.AddMinutes(1), Assert.Single(data.StoredPassages).PassedAt);
        Assert.Single(data.StoredBatches);
    });

    [Fact]
    public Task ARejectedReadingCanBeCorrectedInANewBatchWithTheSameIdentity() => WithApi(async (data, api) =>
    {
        var readingId = Guid.NewGuid();
        var wrong = Reading(readingId);
        wrong["battery"] = 140;
        using var first = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [wrong], []));
        using var corrected = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [Reading(readingId)], []));

        Assert.Equal(readingId, Assert.Single((await Json(first)).GetProperty("rejectedRecords").EnumerateArray())
            .GetProperty("clientRecordId").GetGuid());
        Assert.Equal(HttpStatusCode.Accepted, corrected.StatusCode);
        Assert.Equal([readingId], Guids(await Json(corrected), "acceptedRecordIds"));
        Assert.Equal(85, Assert.Single(data.StoredReadings).Battery);
        Assert.Equal(2, data.StoredBatches.Count);
    });

    [Fact]
    public Task AnInvalidPassageDoesNotEnableTheNextCheckpoint() => WithApi(async (data, api) =>
    {
        var (invalid, dependent) = (Guid.NewGuid(), Guid.NewGuid());
        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [],
        [
            Passage(invalid, First, data.Session.StartedAt.AddMinutes(-30)),
            Passage(dependent, Second, CapturedAt.AddMinutes(5))
        ]));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var rejected = Rejected(await Json(response));
        Assert.Equal("Passage.InvalidPassedAt", rejected[invalid]);
        Assert.Equal("Passage.OutOfSequence", rejected[dependent]);
        Assert.Empty(data.StoredPassages);
    });

    [Fact]
    public Task OfflinePassagesAreValidatedInCaptureOrderNotInArrivalOrder() => WithApi(async (data, api) =>
    {
        var (first, second) = (Guid.NewGuid(), Guid.NewGuid());
        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [],
            [Passage(second, Second, CapturedAt.AddMinutes(5)), Passage(first, First, CapturedAt.AddMinutes(1))]));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(new[] { first, second }.Order(), Guids(await Json(response), "acceptedRecordIds"));
        Assert.Equal([First, Second], data.StoredPassages.OrderBy(passage => passage.PassedAt)
            .Select(passage => passage.CheckpointId));
    });

    [Theory]
    [InlineData("missingPrevious")]
    [InlineData("passedBeforePrevious")]
    public Task APassageBeforeThePreviousCheckpointIsOutOfSequence(string fault) => WithApi(async (data, api) =>
    {
        var later = Guid.NewGuid();
        object[] passages = fault == "missingPrevious"
            ? [Passage(later, Second, CapturedAt.AddMinutes(5))]
            : [Passage(Guid.NewGuid(), First, CapturedAt.AddMinutes(5)), Passage(later, Second, CapturedAt.AddMinutes(5).AddSeconds(-1))];

        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [], passages));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("Passage.OutOfSequence", Rejected(await Json(response))[later]);
        Assert.DoesNotContain(data.StoredPassages, passage => passage.CheckpointId == Second);
    });

    [Fact]
    public Task ALateTransmissionKeepsTheOriginalPassageTime() => WithApi(async (data, api) =>
    {
        var passedAt = DateTimeOffset.FromUnixTimeSeconds(data.Session.StartedAt.AddMinutes(2).ToUnixTimeSeconds());
        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [], [Passage(Guid.NewGuid(), First, passedAt)]));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var passage = Assert.Single(data.StoredPassages);
        Assert.Equal(passedAt, passage.PassedAt);
        Assert.True(passage.SyncedAt - passage.PassedAt > TimeSpan.FromMinutes(30));
    });

    [Theory]
    [InlineData("assigned", HttpStatusCode.Created)]
    [InlineData("otherGuide", HttpStatusCode.NotFound)]
    [InlineData("outOfSequence", HttpStatusCode.Conflict)]
    [InlineData("future", HttpStatusCode.BadRequest)]
    [InlineData("beforeSession", HttpStatusCode.BadRequest)]
    public Task DirectPassageAppliesTheSameGuideTimeAndSequenceRules(string fault, HttpStatusCode expected) =>
        WithApi(async (data, api) =>
        {
            var (checkpoint, passedAt) = fault switch
            {
                "outOfSequence" => (Second, CapturedAt),
                "future" => (First, DateTimeOffset.UtcNow.AddMinutes(10)),
                "beforeSession" => (First, data.Session.StartedAt.AddMinutes(-10)),
                _ => (First, CapturedAt)
            };

            using var response = await api.Send("POST", PassagePath, fault == "otherGuide" ? OtherGuide : Guide,
                new { sessionId = 501, checkpointId = checkpoint, passedAt });

            Assert.Equal(expected, response.StatusCode);
            Assert.Equal(expected == HttpStatusCode.Created ? 1 : 0, data.StoredPassages.Count);
        });

    private static Dictionary<Guid, string?> Rejected(JsonElement body) =>
        body.GetProperty("rejectedRecords").EnumerateArray().ToDictionary(
            record => record.GetProperty("clientRecordId").GetGuid(), record => record.GetProperty("code").GetString());

    private static object Batch(Guid clientBatchId, object[] readings, object[] passages) =>
        new { clientBatchId, sessionId = 501, checkpointId = First, readings, passages };

    private static Dictionary<string, object?> Reading(Guid id) => new()
    {
        ["clientRecordId"] = id, ["wearableDeviceId"] = 70, ["recordedAt"] = CapturedAt, ["latitude"] = -9.5,
        ["longitude"] = -77.5, ["altitude"] = 3000, ["heartRate"] = 88, ["battery"] = 85, ["deviationMeters"] = 12.5
    };

    private static object Passage(Guid id, int checkpoint, DateTimeOffset? passedAt = null) =>
        new { clientRecordId = id, checkpointId = checkpoint, passedAt = passedAt ?? CapturedAt.AddMinutes(1) };

    private static Guid[] Guids(JsonElement body, string property) =>
        body.GetProperty(property).EnumerateArray().Select(item => item.GetGuid()).ToArray();

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static async Task WithApi(Func<SyncBatchData, SyncBatchApi, Task> assertion)
    {
        var data = await SyncBatchData.CreateAsync();
        await using var api = await SyncBatchApi.StartAsync(data);
        await assertion(data, api);
    }
}
