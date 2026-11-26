using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Application.Internal.QueryServices;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.FieldTracking.Interfaces.Rest;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Infrastructure.Pipeline.OpenApi;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Services;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Interfaces.Asp.Configuration;
using VitalTrek.API.Tests.Support;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.CheckpointSync.Application.ACL;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

/// <summary>Real HTTP pipeline using the production controllers, IAM middleware, token validation and services.</summary>
public sealed class TrackingAuthorizationHttpTests(TrackingApiFixture fixture) : IClassFixture<TrackingApiFixture>
{
    [Theory]
    [InlineData("GET", "/api/v1/tracking-sessions")]
    [InlineData("GET", "/api/v1/tracking-sessions/11")]
    [InlineData("POST", "/api/v1/tracking-sessions")]
    [InlineData("GET", "/api/v1/tracking-sessions/11/telemetry-records")]
    [InlineData("POST", "/api/v1/tracking-sessions/11/telemetry-records")]
    public async Task MissingTokenReturns401InsteadOf500(string method, string path)
    {
        using var response = await fixture.Send(method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("expired")]
    [InlineData("invalid-signature")]
    public async Task InvalidTokenReturns401(string kind)
    {
        using var response = await fixture.Send("GET", "/api/v1/tracking-sessions", token: fixture.InvalidToken(kind));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task AdministratorAndTouristCannotStartOrUpload(int userId)
    {
        foreach (var (method, path) in new[]
        {
            ("POST", "/api/v1/tracking-sessions"),
            ("POST", "/api/v1/tracking-sessions/11/telemetry-records")
        })
        {
            using var response = await fixture.Send(method, path, userId);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("/api/v1/tracking-sessions")]
    [InlineData("/api/v1/tracking-sessions/11/telemetry-records")]
    public async Task TouristCannotReadOtherParticipantsRawTracking(string path)
    {
        using var response = await fixture.Send("GET", path, 4);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(5, 1)]
    public async Task SessionListIsScopedToAgencyAndAssignedGuide(int userId, int expectedCount)
    {
        using var response = await fixture.Send("GET", "/api/v1/tracking-sessions?agencyId=999", userId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCount, json.RootElement.GetArrayLength());
        var actor = fixture.Data.Actor(userId);
        Assert.All(json.RootElement.EnumerateArray(), item => Assert.Equal(actor.AgencyId, item.GetProperty("agencyId").GetInt32()));
        if (userId == 2) Assert.Equal(101, json.RootElement[0].GetProperty("expeditionGroupId").GetInt32());
        if (userId == 3) Assert.Equal(102, json.RootElement[0].GetProperty("expeditionGroupId").GetInt32());
    }

    [Theory]
    [InlineData(1, 21)]
    [InlineData(2, 12)]
    [InlineData(2, 21)]
    [InlineData(5, 11)]
    [InlineData(1, 999)]
    public async Task SessionAndTelemetryOutsideScopeReturn404(int userId, int sessionId)
    {
        using var session = await fixture.Send("GET", $"/api/v1/tracking-sessions/{sessionId}", userId);
        using var records = await fixture.Send("GET", $"/api/v1/tracking-sessions/{sessionId}/telemetry-records", userId);
        Assert.Equal(HttpStatusCode.NotFound, session.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, records.StatusCode);
    }

    [Fact]
    public async Task AssignedGuideCanReadOwnSessionAndTelemetry()
    {
        using var session = await fixture.Send("GET", "/api/v1/tracking-sessions/11", 2);
        using var records = await fixture.Send("GET", "/api/v1/tracking-sessions/11/telemetry-records", 2);
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        Assert.Equal(HttpStatusCode.OK, records.StatusCode);
        using var json = JsonDocument.Parse(await records.Content.ReadAsStringAsync());
        Assert.Equal(1001, Assert.Single(json.RootElement.EnumerateArray()).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ForgedBodyAgencyCannotAuthorizeAnotherGroup()
    {
        using var response = await fixture.Send("POST", "/api/v1/tracking-sessions", 2,
            new { expeditionGroupId = 201, agencyId = 2, fieldGuideId = 20 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, fixture.Data.Writes);
        Assert.Equal(0, fixture.Data.Commits);
    }

    [Theory]
    [InlineData("POST", "/api/v1/tracking-sessions/12/telemetry-records")]
    public async Task UnassignedGuideCannotMutateAnotherSession(string method, string path)
    {
        using var response = await fixture.Send(method, path, 2);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, fixture.Data.Writes);
        Assert.Equal(0, fixture.Data.Commits);
    }

    [Fact]
    public async Task OpenApiMarksTrackingOperationsWithBearerAuthentication()
    {
        using var response = await fixture.Send("GET", "/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        foreach (var (path, verb) in new[]
        {
            ("/api/v1/tracking-sessions", "get"), ("/api/v1/tracking-sessions", "post"),
            ("/api/v1/tracking-sessions/{id}", "get"),
            ("/api/v1/tracking-sessions/{sessionId}/telemetry-records", "get"),
            ("/api/v1/tracking-sessions/{sessionId}/telemetry-records", "post")
        })
            Assert.True(paths.GetProperty(path).GetProperty(verb).GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }
}

public sealed class TrackingApiFixture : IAsyncLifetime
{
    private const string Secret = "vitaltrek-m01-test-signing-key-only-not-a-production-secret-1234567890";
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    internal TrackingAccessData Data { get; } = new();
    private readonly TrackingStartData? _startData;
    private readonly TelemetryIngestionData? _telemetryData;

    public TrackingApiFixture() { }
    internal TrackingApiFixture(TrackingStartData startData) => _startData = startData;
    internal TrackingApiFixture(TelemetryIngestionData telemetryData)
    {
        _telemetryData = telemetryData;
        _startData = telemetryData.Ready;
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
            .AddApplicationPart(typeof(TrackingSessionsController).Assembly);
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(BearerSecuritySchemeTransformer.TransformDocumentAsync);
            options.AddOperationTransformer(BearerSecuritySchemeTransformer.TransformOperationAsync);
        });
        builder.Services.Configure<TokenSettings>(settings => settings.Secret = Secret);
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddSingleton(InterfaceStub<IUserQueryService>.Create((method, args) =>
            args[0] is GetUserByIdQuery query
                ? Task.FromResult(Data.Users.FirstOrDefault(user => user.Id == query.Id))
                : throw new InvalidOperationException(method.Name)));
        builder.Services.AddSingleton<IExpeditionSetupContextFacade>(_startData?.Expeditions ?? Data.Expeditions);
        builder.Services.AddSingleton<ITrackingSessionRepository>(_startData?.Sessions ?? Data.SessionRepository);
        builder.Services.AddSingleton<ITelemetryRecordRepository>(_telemetryData?.Repository ?? Data.TelemetryRepository);
        builder.Services.AddSingleton<IUnitOfWork>(_telemetryData?.UnitOfWork ?? _startData?.UnitOfWork ?? Data.UnitOfWork);
        builder.Services.AddScoped<ITrackingSessionQueryService, TrackingSessionQueryService>();
        builder.Services.AddScoped<ITelemetryRecordQueryService, TelemetryRecordQueryService>();
        builder.Services.AddScoped<ITrackingSessionCommandService, TrackingSessionCommandService>();
        builder.Services.AddScoped<ITelemetryRecordCommandService, TelemetryRecordCommandService>();
        builder.Services.AddScoped<IFieldTrackingContextFacade, FieldTrackingContextFacade>();
        builder.Services.AddScoped<ICheckpointSyncContextFacade>(provider => new CheckpointSyncContextFacade(
            _telemetryData?.PassageRepository ?? InterfaceStub<ICheckpointPassageRepository>.Create(),
            provider.GetRequiredService<IFieldTrackingContextFacade>()));
        builder.Services.AddScoped<ITrackingProgressQueryService, TrackingProgressQueryService>();
        _app = builder.Build();
        _app.UseExceptionHandler();
        _app.UseRequestAuthorization();
        _app.UseAuthorization();
        _app.MapControllers();
        _app.MapOpenApi();
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
    }

    public Task<HttpResponseMessage> Send(string method, string path, int? userId = null, object? body = null,
        string? token = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (userId is not null)
            token = _app.Services.GetRequiredService<ITokenService>().GenerateToken(Data.Users.Single(user => user.Id == userId));
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (method == "POST") request.Content = JsonContent.Create(body ?? (path.EndsWith("telemetry-records")
            ? (object)new { wearableDeviceId = 100, records = new[] { new { latitude = -9.5, longitude = -77.5,
                altitude = 3000, recordedAt = DateTimeOffset.UtcNow.AddMinutes(-1), clientRecordId = Guid.NewGuid(),
                heartRate = 88, battery = 85, deviationMeters = 0 } } }
            : new { expeditionGroupId = 101 }));
        return SendAndDispose(request);
    }

    private async Task<HttpResponseMessage> SendAndDispose(HttpRequestMessage request)
    {
        using (request) return await _client.SendAsync(request);
    }

    public string InvalidToken(string kind)
    {
        if (kind == "malformed") return "not-a-jwt";
        var key = kind == "invalid-signature" ? new string('x', 64) : Secret;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Sid, "1")]),
            IssuedAt = DateTime.UtcNow.AddHours(-2), NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = kind == "expired" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature)
        });
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }
}
