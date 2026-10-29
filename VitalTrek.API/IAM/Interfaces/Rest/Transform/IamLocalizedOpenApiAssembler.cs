using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;
using VitalTrek.API.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>
///     Assembles localized OpenAPI metadata (summaries, descriptions and response texts)
///     for the IAM REST endpoints and resources.
/// </summary>
public static class IamLocalizedOpenApiAssembler
{
    private static readonly Dictionary<string, (string Summary, string Description,
        IReadOnlyDictionary<string, string> Responses)> OperationResources = new()
    {
        ["SignUp"] = ("SignUpSummary", "SignUpDescription", new Dictionary<string, string>
        {
            ["201"] = "SignUpCreated", ["400"] = "SignUpBadRequest", ["409"] = "SignUpConflict",
            ["500"] = "OperationServerError"
        }),
        ["SignIn"] = ("SignInSummary", "SignInDescription", new Dictionary<string, string>
        {
            ["200"] = "SignInSuccess", ["400"] = "OperationBadRequest", ["401"] = "SignInUnauthorized",
            ["500"] = "OperationServerError"
        }),
        ["GetAllUsers"] = ("GetAllUsersSummary", "GetAllUsersDescription", new Dictionary<string, string>
        {
            ["200"] = "GetAllUsersSuccess", ["401"] = "OperationUnauthorized", ["403"] = "OperationForbidden"
        }),
        ["GetUserById"] = ("GetUserByIdSummary", "GetUserByIdDescription", new Dictionary<string, string>
        {
            ["200"] = "GetUserByIdSuccess", ["401"] = "OperationUnauthorized", ["404"] = "GetUserByIdNotFound"
        }),
        ["CreateUser"] = ("CreateUserSummary", "CreateUserDescription", new Dictionary<string, string>
        {
            ["201"] = "CreateUserCreated", ["400"] = "CreateUserBadRequest", ["401"] = "OperationUnauthorized",
            ["403"] = "OperationForbidden", ["409"] = "CreateUserConflict", ["500"] = "OperationServerError"
        })
    };

    private static readonly Dictionary<Type, string> SchemaResources = new()
    {
        [typeof(SignUpResource)] = "SignUpSchema",
        [typeof(SignInResource)] = "SignInSchema",
        [typeof(CreateUserResource)] = "CreateUserSchema",
        [typeof(UserResource)] = "UserSchema",
        [typeof(AuthenticatedUserResource)] = "AuthenticatedUserSchema"
    };

    /// <summary>
    ///     Localizes the summary, description, operation id and response descriptions of known operations.
    /// </summary>
    /// <param name="operation">The OpenAPI operation being transformed.</param>
    /// <param name="context">The transformer context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public static Task TransformOperationAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            !action.ControllerTypeInfo.Namespace!.StartsWith("VitalTrek.API.IAM", StringComparison.Ordinal) ||
            !OperationResources.TryGetValue(action.ActionName, out var resources))
            return Task.CompletedTask;

        var localizer = context.ApplicationServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        operation.OperationId = action.ActionName;
        operation.Summary = localizer[resources.Summary].Value;
        operation.Description = localizer[resources.Description].Value;
        if (operation.Responses is null) return Task.CompletedTask;

        foreach (var (statusCode, resourceKey) in resources.Responses)
        {
            if (operation.Responses.TryGetValue(statusCode, out var response) && response is OpenApiResponse openApiResponse)
                openApiResponse.Description = localizer[resourceKey].Value;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Localizes the description of known IAM resource schemas.
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
