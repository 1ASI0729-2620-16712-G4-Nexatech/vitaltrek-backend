using System.Net;
using System.Text.Json;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TrackingProgressHttpTests
{
    private const string SessionPath = "/api/v1/tracking-sessions/501/progress";
    private const string GroupPath = "/api/v1/expedition-groups/101/tracking-progress";

    [Theory]
    [InlineData("/api/v1/tracking-sessions/501/progress")]
    [InlineData("/api/v1/expedition-groups/101/tracking-progress")]
    public Task AnonymousProgressReturns401(string path) => WithApi(async (_, api) =>
    {
        using var response = await api.Send("GET", path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    });

    [Theory]
    [InlineData("expired")]
    [InlineData("invalid-signature")]
    public Task InvalidTokensCannotReadProgress(string kind) => WithApi(async (_, api) =>
    {
        using var response = await api.Send("GET", SessionPath, token: api.InvalidToken(kind));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    });

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public Task OtherGuideOrAgencyCannotReadSessionOrGroupProgress(int userId) => WithApi(async (_, api) =>
    {
        using var bySession = await api.Send("GET", SessionPath, userId);
        using var byGroup = await api.Send("GET", GroupPath, userId);
        Assert.Equal(HttpStatusCode.NotFound, bySession.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, byGroup.StatusCode);
        using var json = JsonDocument.Parse(await byGroup.Content.ReadAsStringAsync());
        Assert.Equal("Tracking.ProgressNotFound", json.RootElement.GetProperty("code").GetString());
    });

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(4, 1)]
    public Task ProgressProjectsOnlyTheParticipantsVisibleToTheActor(int userId, int participants) => WithApi(async (data, api) =>
    {
        SeedReadings(data);
        using var response = await api.Send("GET", SessionPath + "?agencyId=999", userId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        var members = json.RootElement.GetProperty("participants");
        Assert.Equal(participants, members.GetArrayLength());
        if (userId == 4)
        {
            Assert.Equal(60, members[0].GetProperty("memberId").GetInt32());
            Assert.DoesNotContain("\"heartRate\":199", text);
        }
        Assert.DoesNotContain("emergencyContact", text);
        Assert.DoesNotContain("identityDocument", text);
    });

    [Fact]
    public Task RefreshDoesNotInventANewerSynchronizationTime() => WithApi(async (data, api) =>
    {
        SeedReadings(data);
        var synced = data.Stored.Max(record => record.SynchronizedAt);
        data.Passages.Add(Passage(30, synced.AddMinutes(-5), synced.AddMinutes(-4)));
        using var first = await api.Send("GET", SessionPath, 2);
        using var second = await api.Send("GET", GroupPath, 2);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var body = await first.Content.ReadAsStringAsync();
        Assert.Equal(body, await second.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(body);
        Assert.Equal(synced, json.RootElement.GetProperty("synchronizedAt").GetDateTimeOffset());
        Assert.Equal(50, json.RootElement.GetProperty("estimatedProgressPercentage").GetInt32());
        Assert.Equal(30, json.RootElement.GetProperty("lastConfirmedCheckpoint").GetProperty("checkpointId").GetInt32());
        Assert.Equal(31, json.RootElement.GetProperty("nextCheckpoint").GetProperty("checkpointId").GetInt32());
    });

    [Fact]
    public Task EmptyProgressHasNoSyntheticSynchronizationTimeOrRiskLevel() => WithApi(async (_, api) =>
    {
        using var response = await api.Send("GET", SessionPath, 2);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("synchronizedAt").ValueKind);
        Assert.Equal(0, json.RootElement.GetProperty("confirmedCheckpointsCount").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("riskLevel", out var riskLevel));
    });

    [Fact]
    public Task LaterCheckpointsAndForeignRoutePassagesCannotFillAnEarlierGap() => WithApi(async (data, api) =>
    {
        var now = DateTimeOffset.UtcNow;
        data.Passages.Add(Passage(31, now.AddMinutes(-3), now.AddMinutes(-2)));
        data.Passages.Add(Passage(999, now.AddMinutes(-3), now.AddMinutes(-2)));
        using var response = await api.Send("GET", SessionPath, 2);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetProperty("estimatedProgressPercentage").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("lastConfirmedCheckpoint").ValueKind);
    });

    [Fact]
    public Task TouristOutsideTheManifestCannotReadAnyProgress() => WithApi(async (_, api) =>
    {
        TrackingStartData.Set(api.Data.Users.Single(user => user.Id == 4), "IdentityDocument", "UNREGISTERED");
        using var response = await api.Send("GET", SessionPath, 4);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    });

    private static void SeedReadings(TelemetryIngestionData data)
    {
        var received = DateTimeOffset.UtcNow.AddMinutes(-2);
        data.Stored.Add(TrackingStartData.WithId(new TelemetryRecord(501, 70, data.Reading(), 60, received), 1));
        data.Stored.Add(TrackingStartData.WithId(new TelemetryRecord(501, 71, data.Reading() with { HeartRate = 199 }, 61,
            received.AddSeconds(1)), 2));
    }

    private static CheckpointPassage Passage(int checkpointId, DateTimeOffset passedAt, DateTimeOffset synchronizedAt) =>
        new(new RecordCheckpointPassageCommand(1, 10, 501, checkpointId, passedAt), synchronizedAt);

    private static async Task WithApi(Func<TelemetryIngestionData, TrackingApiFixture, Task> assertion)
    {
        var data = await TelemetryIngestionData.CreateAsync();
        var api = new TrackingApiFixture(data);
        TrackingStartData.Set(api.Data.Users.Single(user => user.Id == 4), "IdentityDocument", "DOC-0");
        await api.InitializeAsync();
        try { await assertion(data, api); }
        finally { await api.DisposeAsync(); }
    }
}
