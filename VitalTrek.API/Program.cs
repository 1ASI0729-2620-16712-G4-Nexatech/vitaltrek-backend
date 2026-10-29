using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Scalar.AspNetCore;
using VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;
using VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Seeding;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Application.ACL;
using VitalTrek.API.IAM.Application.Internal.CommandServices;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Application.Internal.QueryServices;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Repositories;
using VitalTrek.API.IAM.Infrastructure.Hashing.BCrypt.Services;
using VitalTrek.API.IAM.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.IAM.Infrastructure.Persistence.EntityFrameworkCore.Seeding;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Infrastructure.Pipeline.OpenApi;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Services;
using VitalTrek.API.IAM.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.Rest.Transform;
using VitalTrek.API.Resources;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Interfaces.Asp.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Localization Configuration
builder.Services.AddLocalization();

// Add services to the container.
builder.Services.AddRouting(options => options.LowercaseUrls = false);
builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var localizer = context.HttpContext.RequestServices
                .GetRequiredService<IStringLocalizer<SharedResource>>();
            var problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();
            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext,
                context.ModelState,
                statusCode: StatusCodes.Status400BadRequest,
                title: localizer["InvalidModelStateTitle"].Value);

            var result = new BadRequestObjectResult(problemDetails);
            result.ContentTypes.Add("application/problem+json");
            return result;
        };
    });

// Register RFC 7807 ProblemDetails payloads for centralized exception handling.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status is null or >= 500)
        {
            var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
            context.ProblemDetails.Title ??= localizer["UnexpectedServerError"].Value;
            context.ProblemDetails.Detail ??= localizer["UnexpectedErrorProcessingRequest"].Value;
        }
    };
});

// Generate the OpenAPI document with ASP.NET Core's built-in support.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "VitalTrek Platform API";
        document.Info.Version = "v1";
        document.Info.Description =
            "RESTful API for VitalTrek, a safety and operations platform for adventure tourism in low-connectivity areas.";
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer(BearerSecuritySchemeTransformer.TransformDocumentAsync);
    options.AddOperationTransformer(BearerSecuritySchemeTransformer.TransformOperationAsync);
    options.AddOperationTransformer(IamLocalizedOpenApiAssembler.TransformOperationAsync);
    options.AddSchemaTransformer(IamLocalizedOpenApiAssembler.TransformSchemaAsync);
    options.AddOperationTransformer(ExpeditionSetupLocalizedOpenApiAssembler.TransformOperationAsync);
    options.AddSchemaTransformer(ExpeditionSetupLocalizedOpenApiAssembler.TransformSchemaAsync);
});

// CORS Configuration for the VitalTrek Web Applications.
const string allowAllCorsPolicy = "AllowAllPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(allowAllCorsPolicy, policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Configure Database Context and route EF logs through the app logger pipeline.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionStringTemplate))
        throw new InvalidOperationException("Database connection string is not set in the configuration.");

    var connectionString = Environment.ExpandEnvironmentVariables(connectionStringTemplate);
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Database connection string is not set in the configuration.");

    options.UseMySQL(connectionString)
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// Configure Dependency Injection

// Shared Bounded Context Injection Configuration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// IAM Bounded Context Injection Configuration
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.PostConfigure<TokenSettings>(settings =>
    settings.Secret = Environment.ExpandEnvironmentVariables(settings.Secret));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAgencyRepository, AgencyRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

// Expedition Setup Bounded Context Injection Configuration
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<ICheckpointRepository, CheckpointRepository>();
builder.Services.AddScoped<IExpectedTimeWindowRepository, ExpectedTimeWindowRepository>();
builder.Services.AddScoped<IRouteNoteRepository, RouteNoteRepository>();
builder.Services.AddScoped<ISafetyProtocolRepository, SafetyProtocolRepository>();
builder.Services.AddScoped<IRouteCommandService, RouteCommandService>();
builder.Services.AddScoped<IRouteQueryService, RouteQueryService>();
builder.Services.AddScoped<ICheckpointCommandService, CheckpointCommandService>();
builder.Services.AddScoped<ICheckpointQueryService, CheckpointQueryService>();
builder.Services.AddScoped<IExpectedTimeWindowCommandService, ExpectedTimeWindowCommandService>();
builder.Services.AddScoped<IExpectedTimeWindowQueryService, ExpectedTimeWindowQueryService>();
builder.Services.AddScoped<IRouteNoteCommandService, RouteNoteCommandService>();
builder.Services.AddScoped<IRouteNoteQueryService, RouteNoteQueryService>();
builder.Services.AddScoped<ISafetyProtocolCommandService, SafetyProtocolCommandService>();
builder.Services.AddScoped<ISafetyProtocolQueryService, SafetyProtocolQueryService>();

var app = builder.Build();

// Apply pending migrations on startup (safe to call even when schema is up to date)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate();

    // Demonstration data is only inserted into an empty database.
    IamDemoDataSeeder.Seed(context, services.GetRequiredService<IHashingService>());
    var demoAgencyId = context.Set<User>()
        .Where(user => user.Username == new Username("admin@vitaltrek.demo"))
        .Select(user => user.AgencyId)
        .FirstOrDefault();
    ExpeditionSetupDemoDataSeeder.Seed(context, demoAgencyId);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Localization must run before OpenAPI so generated documentation follows the request culture.
string[] supportedCultures = ["en-US", "en", "es-419", "es"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.ApplyCurrentCultureToResponseHeaders = true;
app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();

app.UseCors(allowAllCorsPolicy);

// Resolve the authenticated user from the bearer token before reaching the controllers.
app.UseRequestAuthorization();

app.UseAuthorization();

// OpenAPI and Scalar are kept available in all environments to expose the API reference.
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();
