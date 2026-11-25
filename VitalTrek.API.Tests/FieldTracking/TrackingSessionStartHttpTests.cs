using System.Net;
using System.Text.Json;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TrackingSessionStartHttpTests
{
    [Fact]
    public async Task PreparedStartReturns201WithLocationAndRetryReturns200WithSameSession()
    {
        var data = new TrackingStartData();
        var api = new TrackingApiFixture(data);
        await api.InitializeAsync();
        try
        {
            using var created = await api.Send("POST", "/api/v1/tracking-sessions", 2);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.EndsWith("/api/v1/tracking-sessions/501", created.Headers.Location!.ToString());
            using var retry = await api.Send("POST", "/api/v1/tracking-sessions", 2);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(await created.Content.ReadAsStringAsync(), await retry.Content.ReadAsStringAsync());
            Assert.Equal(ExpeditionGroupStatus.Active, data.Group.Status);
            Assert.Equal(1, data.Saves);
        }
        finally { await api.DisposeAsync(); }
    }

    [Theory]
    [InlineData("draft-route")]
    [InlineData("missing-segment")]
    [InlineData("missing-protocol")]
    [InlineData("missing-preparation")]
    [InlineData("revoked-consent")]
    public async Task UnpreparedStartReturns409AndLeavesGroupScheduled(string fault)
    {
        var data = new TrackingStartData();
        TrackingSessionStartTests.ApplyFault(data, fault);
        var api = new TrackingApiFixture(data);
        await api.InitializeAsync();
        try
        {
            using var response = await api.Send("POST", "/api/v1/tracking-sessions", 2);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.True(data.Group.IsScheduled);
            Assert.Null(data.Session);
            Assert.Equal(0, data.Saves);
        }
        finally { await api.DisposeAsync(); }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task DirectEndEndpointIsUnavailableAndCannotChangeTheSession(int? userId)
    {
        var api = new TrackingApiFixture();
        await api.InitializeAsync();
        try
        {
            using var response = await api.Send("PATCH", "/api/v1/tracking-sessions/11/end", userId);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(api.Data.Sessions.Single(session => session.Id == 11).IsActive);
            Assert.Equal(0, api.Data.Commits);
            using var openapi = await api.Send("GET", "/openapi/v1.json");
            using var json = JsonDocument.Parse(await openapi.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("paths").TryGetProperty("/api/v1/tracking-sessions/{id}/end", out _));
        }
        finally { await api.DisposeAsync(); }
    }
}
