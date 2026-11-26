using System.Net;
using System.Text.Json;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TelemetryIngestionHttpTests
{
    private const string Path = "/api/v1/tracking-sessions/501/telemetry-records";

    [Fact]
    public Task CaptureAndRetryPreserveAllFieldsAndIgnoreForgedParticipantIdentity() => WithApi(async (data, api) =>
    {
        var entry = data.Reading();
        using var first = await api.Send("POST", Path, 2, Body(entry));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var retry = await api.Send("POST", Path, 2, Body(entry));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await retry.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var reading = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(entry.ClientRecordId, reading.GetProperty("clientRecordId").GetGuid());
        Assert.Equal(60, reading.GetProperty("memberId").GetInt32());
        Assert.Equal(88, reading.GetProperty("heartRate").GetInt32());
        Assert.Equal(85, reading.GetProperty("battery").GetInt32());
        Assert.Equal(12.5, reading.GetProperty("deviationMeters").GetDouble());
        Assert.True(reading.GetProperty("isDeviationSimulated").GetBoolean());
        Assert.True(reading.GetProperty("recordedAt").GetDateTimeOffset() <
                    reading.GetProperty("synchronizedAt").GetDateTimeOffset().AddMinutes(-9));
        Assert.Single(data.Stored);
        Assert.Equal(1, data.Saves);
    });

    [Fact]
    public Task ChangedPayloadForTheSameIdentityReturns409AndAStableCode() => WithApi(async (data, api) =>
    {
        var entry = data.Reading();
        using var first = await api.Send("POST", Path, 2, Body(entry));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var conflict = await api.Send("POST", Path, 2, Body(entry with { HeartRate = 99 }));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var json = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        Assert.Equal("Telemetry.RecordIdentityConflict", json.RootElement.GetProperty("code").GetString());
        Assert.Equal(88, Assert.Single(data.Stored).HeartRate);
    });

    [Theory]
    [InlineData("consent", HttpStatusCode.Conflict)]
    [InlineData("wearable", HttpStatusCode.NotFound)]
    [InlineData("identity", HttpStatusCode.BadRequest)]
    [InlineData("closed", HttpStatusCode.Conflict)]
    public Task InvalidOrUnauthorizedCaptureIsNotPersisted(string fault, HttpStatusCode expected) => WithApi(async (data, api) =>
    {
        var entry = data.Reading();
        if (fault == "consent") data.Ready.Preparations[0].RevokeConsent(DateTimeOffset.UtcNow);
        if (fault == "identity") entry = entry with { ClientRecordId = Guid.Empty };
        if (fault == "closed") data.Ready.Session!.End();
        using var response = await api.Send("POST", Path, 2, Body(entry, fault == "wearable" ? 999 : 70));
        Assert.Equal(expected, response.StatusCode);
        Assert.Empty(data.Stored);
        Assert.Equal(0, data.Saves);
    });

    private static object Body(TelemetryRecordEntry entry, int wearable = 70) => new
    {
        wearableDeviceId = wearable, memberId = 999, agencyId = 999,
        records = new[] { new { entry.Latitude, entry.Longitude, entry.Altitude, entry.RecordedAt,
            entry.ClientRecordId, entry.HeartRate, entry.Battery, entry.DeviationMeters, memberId = 999 } }
    };

    [Theory]
    [InlineData("battery")]
    [InlineData("deviationMeters")]
    [InlineData("latitude")]
    [InlineData("clientRecordId")]
    public Task MissingCaptureFieldsAreRejectedInsteadOfInventingZeroValues(string field) => WithApi(async (data, api) =>
    {
        var entry = data.Reading();
        var reading = new Dictionary<string, object?>
        {
            ["latitude"] = entry.Latitude, ["longitude"] = entry.Longitude, ["altitude"] = entry.Altitude,
            ["recordedAt"] = entry.RecordedAt, ["clientRecordId"] = entry.ClientRecordId,
            ["heartRate"] = entry.HeartRate, ["battery"] = entry.Battery, ["deviationMeters"] = entry.DeviationMeters
        };
        reading.Remove(field);
        using var response = await api.Send("POST", Path, 2, new { wearableDeviceId = 70, records = new[] { reading } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(data.Stored);
        Assert.Equal(0, data.Saves);
    });

    private static async Task WithApi(Func<TelemetryIngestionData, TrackingApiFixture, Task> assertion)
    {
        var data = await TelemetryIngestionData.CreateAsync();
        var api = new TrackingApiFixture(data);
        await api.InitializeAsync();
        try { await assertion(data, api); }
        finally { await api.DisposeAsync(); }
    }
}
