using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;
using VitalTrek.API.Resources;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;

/// <summary>
///     Assembles localized OpenAPI metadata for the Expedition Setup REST endpoints and resources.
/// </summary>
public static class ExpeditionSetupLocalizedOpenApiAssembler
{
    private static readonly string[] Operations =
    [
        "GetAllRoutes", "GetRouteById", "CreateRoute", "UpdateRouteStatus",
        "GetAllCheckpoints", "GetCheckpointById", "CreateCheckpoint",
        "GetAllExpectedTimeWindows", "GetExpectedTimeWindowById", "CreateExpectedTimeWindow",
        "GetAllRouteNotes", "GetRouteNoteById", "CreateRouteNote", "UpdateRouteNote",
        "GetAllSafetyProtocols", "GetSafetyProtocolById", "CreateSafetyProtocol", "UpdateSafetyProtocol"
    ];

    private static readonly Dictionary<string, string> ResponseResources = new()
    {
        ["200"] = "OperationSuccess",
        ["201"] = "OperationCreated",
        ["400"] = "OperationBadRequest",
        ["401"] = "OperationUnauthorized",
        ["403"] = "OperationForbidden",
        ["404"] = "OperationNotFound",
        ["409"] = "OperationConflict",
        ["500"] = "OperationServerError"
    };

    private static readonly Dictionary<Type, string> SchemaResources = new()
    {
        [typeof(RouteResource)] = "RouteSchema",
        [typeof(CreateRouteResource)] = "CreateRouteSchema",
        [typeof(UpdateRouteStatusResource)] = "UpdateRouteStatusSchema",
        [typeof(CheckpointResource)] = "CheckpointSchema",
        [typeof(CreateCheckpointResource)] = "CreateCheckpointSchema",
        [typeof(ExpectedTimeWindowResource)] = "ExpectedTimeWindowSchema",
        [typeof(CreateExpectedTimeWindowResource)] = "CreateExpectedTimeWindowSchema",
        [typeof(RouteNoteResource)] = "RouteNoteSchema",
        [typeof(CreateRouteNoteResource)] = "CreateRouteNoteSchema",
        [typeof(UpdateRouteNoteResource)] = "UpdateRouteNoteSchema",
        [typeof(SafetyProtocolResource)] = "SafetyProtocolSchema",
        [typeof(CreateSafetyProtocolResource)] = "CreateSafetyProtocolSchema",
        [typeof(UpdateSafetyProtocolResource)] = "UpdateSafetyProtocolSchema"
    };

    /// <summary>
    ///     Localizes the summary, description, operation id and response descriptions of the Expedition Setup operations.
    /// </summary>
    /// <param name="operation">The OpenAPI operation being transformed.</param>
    /// <param name="context">The transformer context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public static Task TransformOperationAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            !action.ControllerTypeInfo.Namespace!.StartsWith("VitalTrek.API.ExpeditionSetup", StringComparison.Ordinal) ||
            !Operations.Contains(action.ActionName))
            return Task.CompletedTask;

        var localizer = context.ApplicationServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        operation.OperationId = action.ActionName;
        operation.Summary = localizer[$"{action.ActionName}Summary"].Value;
        operation.Description = localizer[$"{action.ActionName}Description"].Value;
        if (operation.Responses is null) return Task.CompletedTask;

        foreach (var (statusCode, response) in operation.Responses)
        {
            if (ResponseResources.TryGetValue(statusCode, out var resourceKey) && response is OpenApiResponse openApiResponse)
                openApiResponse.Description = localizer[resourceKey].Value;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Localizes the description of the Expedition Setup resource schemas.
    /// </summary>
    /// <param name="schema">The OpenAPI schema being transformed.</param>
    /// <param name="context">The transformer context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public static Task TransformSchemaAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonPropertyInfo is not null ||
            !SchemaResources.TryGetValue(context.JsonTypeInfo.Type, out var resourceKey))
            return Task.CompletedTask;

        var localizer = context.ApplicationServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        schema.Description = localizer[resourceKey].Value;
        return Task.CompletedTask;
    }
}
