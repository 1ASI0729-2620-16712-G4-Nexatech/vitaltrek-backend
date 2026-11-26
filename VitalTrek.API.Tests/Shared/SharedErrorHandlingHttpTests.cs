using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using VitalTrek.API.Shared.Infrastructure.Pipeline.Middleware.Components;
using VitalTrek.API.Shared.Resources;
using Xunit;
using SharedProblemDetailsFactory = VitalTrek.API.Shared.Interfaces.Rest.ProblemDetails.ProblemDetailsFactory;

namespace VitalTrek.API.Tests.Shared;

/// <summary>Checks the shared HTTP components against the real MVC, localization and exception pipelines.</summary>
public sealed class SharedErrorHandlingHttpTests(SharedErrorsApiFixture fixture) : IClassFixture<SharedErrorsApiFixture>
{
    [Theory]
    [InlineData("en", "A valid expedition group identifier is required.")]
    [InlineData("en-US", "A valid expedition group identifier is required.")]
    [InlineData("es", "Se requiere un identificador de grupo válido.")]
    [InlineData("es-419", "Se requiere un identificador de grupo válido.")]
    public async Task MovedResourcesResolveThroughTheSharedMarkerAndCultureFallback(string culture, string expected)
    {
        using var response = await fixture.Send("GET", "message", culture);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("resourceNotFound").GetBoolean());
        Assert.Equal(expected, json.RootElement.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("en", "The request contains invalid values.", "A valid expedition group identifier is required.")]
    [InlineData("es", "La solicitud contiene valores inválidos.", "Se requiere un identificador de grupo válido.")]
    public async Task InvalidModelsKeepLocalizedTitlesFieldErrorsAndTraceIds(string culture, string title, string error)
    {
        using var response = await fixture.Send("POST", "validation", culture);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.Equal(error, Assert.Single(json.RootElement.GetProperty("errors")
            .GetProperty("ExpeditionGroupId").EnumerateArray()).GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Theory]
    [InlineData("en", "application/json", "Unexpected server error",
        "An unexpected error occurred while processing your request.")]
    [InlineData("es", "application/json", "Error inesperado del servidor",
        "Ocurrió un error inesperado al procesar su solicitud.")]
    [InlineData("en-US", "text/plain", "Unexpected server error",
        "An unexpected error occurred while processing your request.")]
    [InlineData("es-419", "text/html", "Error inesperado del servidor",
        "Ocurrió un error inesperado al procesar su solicitud.")]
    public async Task UnexpectedFailuresReturnLocalizedProblemsWithoutInternalExceptionDetails(string culture,
        string accept, string title, string detail)
    {
        using var response = await fixture.Send("GET", "failure", culture, accept);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SharedErrorProbeController.PrivateExceptionMessage, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.Equal(detail, json.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/__tests/shared-errors/failure", json.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task ConcurrentRequestsDoNotShareTheSingletonHandlersCulture()
    {
        var requests = new[] { "es", "en", "es-419", "en-US" }.Select(async culture =>
        {
            using var response = await fixture.Send("GET", "failure", culture);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(culture.StartsWith("es") ? "Error inesperado del servidor" : "Unexpected server error",
                json.RootElement.GetProperty("title").GetString());
        });
        await Task.WhenAll(requests);
    }
}

public sealed class SharedErrorsApiFixture : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddSingleton<SharedProblemDetailsFactory>();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(SharedErrorProbeController).Assembly)
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider =
                (_, factory) => factory.Create(typeof(SharedResource)))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
                context.HttpContext.RequestServices.GetRequiredService<SharedProblemDetailsFactory>()
                    .CreateValidationProblemDetails(context));
        _app = builder.Build();
        string[] cultures = ["en-US", "en", "es-419", "es"];
        var localization = new RequestLocalizationOptions().SetDefaultCulture("en-US")
            .AddSupportedCultures(cultures).AddSupportedUICultures(cultures);
        localization.ApplyCurrentCultureToResponseHeaders = true;
        _app.UseRequestLocalization(localization);
        _app.UseExceptionHandler();
        _app.MapControllers();
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<HttpResponseMessage> Send(string method, string action, string culture,
        string accept = "application/json")
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/__tests/shared-errors/{action}");
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        if (method == "POST") request.Content = JsonContent.Create(new { });
        return await _client.SendAsync(request);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }
}

// These routes exist only in the test assembly; the application never registers this controller.
[ApiController]
[Route("__tests/shared-errors")]
public sealed class SharedErrorProbeController(IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    internal const string PrivateExceptionMessage = "test-only internal failure; never expose this message";

    [HttpGet("message")]
    public IActionResult Message()
    {
        var message = localizer["RequiredExpeditionGroupId"];
        return Ok(new { message = message.Value, message.ResourceNotFound });
    }

    [HttpPost("validation")]
    public IActionResult Validation([FromBody] SharedValidationProbeResource resource) => Ok(resource);

    [HttpGet("failure")]
    public IActionResult Failure() => throw new InvalidOperationException(PrivateExceptionMessage);
}

public sealed class SharedValidationProbeResource
{
    [Required(ErrorMessage = "RequiredExpeditionGroupId")]
    public int? ExpeditionGroupId { get; init; }
}
