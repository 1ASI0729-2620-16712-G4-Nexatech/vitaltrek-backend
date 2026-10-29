using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.Resources;

namespace VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Requires an authenticated user and, optionally, one of the given roles.
///     Responds with 401 when no valid token is present and 403 when the role is not allowed.
/// </summary>
/// <param name="roles">The roles allowed to invoke the endpoint. Empty means any authenticated role.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeAttribute(params Role[] roles) : Attribute, IAuthorizationFilter
{
    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any()) return;

        var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        var user = context.HttpContext.GetAuthenticatedUser();
        if (user is null)
        {
            context.Result = Problem(StatusCodes.Status401Unauthorized, localizer["AuthenticationRequired"].Value);
            return;
        }

        if (roles.Length > 0 && !roles.Contains(user.Role))
            context.Result = Problem(StatusCodes.Status403Forbidden, localizer["AccessForbidden"].Value);
    }

    private static ObjectResult Problem(int statusCode, string title) =>
        new(new ProblemDetails { Status = statusCode, Title = title }) { StatusCode = statusCode };
}
