using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;

namespace VitalTrek.API.IAM.Infrastructure.Pipeline.OpenApi;

/// <summary>
///     Declares the bearer token security scheme in the OpenAPI document and marks the operations that require it,
///     so the API reference can send authenticated requests.
/// </summary>
public static class BearerSecuritySchemeTransformer
{
    private const string SchemeName = "Bearer";

    /// <summary>
    ///     Adds the bearer security scheme to the document components.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="context">The document transformer context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public static Task TransformDocumentAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token returned by POST /api/v1/authentication/sign-in."
        };
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Adds the bearer security requirement to the operations protected by <see cref="AuthorizeAttribute"/>.
    /// </summary>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <param name="context">The operation transformer context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public static Task TransformOperationAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action) return Task.CompletedTask;
        var requiresAuthorization =
            action.ControllerTypeInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Length > 0 ||
            action.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Length > 0;
        var allowsAnonymous = action.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0;
        if (!requiresAuthorization || allowsAnonymous) return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = []
        });
        return Task.CompletedTask;
    }
}
