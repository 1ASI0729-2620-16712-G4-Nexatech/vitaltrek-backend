using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;

namespace VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Middleware that resolves the bearer token of each request and stores the authenticated user
///     in <see cref="HttpContext.Items"/>. Access decisions are taken by the authorization attributes.
/// </summary>
/// <param name="next">The next delegate in the pipeline.</param>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    ///     Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="userQueryService">The user query service.</param>
    /// <param name="tokenService">The token service.</param>
    /// <returns>A task representing the middleware execution.</returns>
    public async Task InvokeAsync(HttpContext context, IUserQueryService userQueryService, ITokenService tokenService)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var userId = await tokenService.ValidateToken(header[BearerPrefix.Length..].Trim());
            if (userId is not null)
            {
                var user = await userQueryService.Handle(new GetUserByIdQuery(userId.Value), context.RequestAborted);
                if (user is not null) context.Items[HttpContextExtensions.AuthenticatedUserKey] = user;
            }
        }

        await next(context);
    }
}
