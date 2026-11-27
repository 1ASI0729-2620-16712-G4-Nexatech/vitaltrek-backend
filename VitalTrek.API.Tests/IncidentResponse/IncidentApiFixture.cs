using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Services;
using VitalTrek.API.IncidentResponse.Application.Internal.CommandServices;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;
using VitalTrek.API.IncidentResponse.Application.Internal.QueryServices;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.IncidentResponse.Interfaces.Rest;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Interfaces.Asp.Configuration;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     Real HTTP pipeline with the production Incident Response controllers, IAM middleware, token validation and
///     application services. Only persistence, the other bounded contexts and the e-mail provider are test doubles.
/// </summary>
public sealed class IncidentApiFixture : IAsyncLifetime
{
    private const string Secret = "vitaltrek-i01-test-signing-key-only-not-a-production-secret-1234567890";
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    internal IncidentWorkflowData Data { get; } = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
            .AddApplicationPart(typeof(IncidentsController).Assembly);
        builder.Services.Configure<TokenSettings>(settings => settings.Secret = Secret);
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddSingleton(InterfaceStub<IUserQueryService>.Create((method, args) =>
            args[0] is GetUserByIdQuery query
                ? Task.FromResult(Data.Users.FirstOrDefault(user => user.Id == query.Id))
                : throw new InvalidOperationException(method.Name)));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.Configure<IncidentNotificationSettings>(settings =>
        {
            settings.AllowedRecipients = [IncidentWorkflowData.AllowedRecipient];
            settings.AttemptTimeoutSeconds = 1;
            settings.AttemptLeaseSeconds = 5;
        });
        builder.Services.AddSingleton<IUnitOfWork>(Data.UnitOfWork);
        builder.Services.AddSingleton<IIncidentRepository>(Data.Incidents);
        builder.Services.AddSingleton<IEvacuationActionRepository>(Data.EvacuationActions);
        builder.Services.AddSingleton<IIncidentNotificationRepository>(Data.Notifications);
        builder.Services.AddSingleton<IIncidentNotificationSender>(Data.Provider);
        builder.Services.AddSingleton(new ExternalTrackingSessionService(Data.FieldTracking()));
        builder.Services.AddSingleton(new ExternalExpeditionSetupService(Data.ExpeditionSetup()));
        builder.Services.AddSingleton(new ExternalSafetyMonitoringService(Data.SafetyMonitoring()));
        builder.Services.AddSingleton(new ExternalIamService(Data.Iam()));
        builder.Services.AddScoped<IIncidentCommandService, IncidentCommandService>();
        builder.Services.AddScoped<IIncidentQueryService, IncidentQueryService>();
        builder.Services.AddScoped<IEvacuationActionCommandService, EvacuationActionCommandService>();
        builder.Services.AddScoped<IEvacuationActionQueryService, EvacuationActionQueryService>();
        builder.Services.AddScoped<IIncidentNotificationCommandService, IncidentNotificationCommandService>();
        builder.Services.AddScoped<IIncidentNotificationQueryService, IncidentNotificationQueryService>();
        _app = builder.Build();
        _app.UseExceptionHandler();
        _app.UseRequestAuthorization();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<(int Status, JsonElement Body)> Send(string method, string path, int? userId = null,
        object? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (userId is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                _app.Services.GetRequiredService<ITokenService>()
                    .GenerateToken(Data.Users.Single(user => user.Id == userId)));
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await _client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        return ((int)response.StatusCode, json);
    }

    /// <summary>Reports a manual incident on session 11 as guide A and returns its identifier.</summary>
    public async Task<int> ReportIncident(int sessionId = 11, int userId = IncidentWorkflowData.GuideA,
        int? alertId = null, string description = "Participant twisted an ankle near checkpoint 2.")
    {
        var (status, body) = await Send("POST", "/api/v1/incidents", userId, new
        {
            sessionId, incidentType = "accident", severity = "high", description,
            reportedAt = DateTimeOffset.UtcNow.AddMinutes(-10), alertId
        });
        Assert.Equal(201, status);
        return body.GetProperty("id").GetInt32();
    }

    /// <summary>Reports, confirms and declares the emergency of an incident, returning its identifier.</summary>
    public async Task<int> ReportEmergency()
    {
        var id = await ReportIncident();
        Assert.Equal(200, (await Send("PATCH", $"/api/v1/incidents/{id}/confirm", IncidentWorkflowData.GuideA)).Status);
        Assert.Equal(200,
            (await Send("PATCH", $"/api/v1/incidents/{id}/declare-emergency", IncidentWorkflowData.Admin)).Status);
        return id;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }
}
