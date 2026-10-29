using VitalTrek.API.IAM.Domain.Model.Aggregates;

namespace VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;

/// <summary>
///     Provides access to the user authenticated by the request authorization middleware.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    ///     Key under which the authenticated user is stored in <see cref="HttpContext.Items"/>.
    /// </summary>
    public const string AuthenticatedUserKey = "User";

    /// <summary>
    ///     Gets the authenticated user of the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The authenticated user, or <see langword="null"/> for anonymous requests.</returns>
    public static User? GetAuthenticatedUser(this HttpContext context) =>
        context.Items.TryGetValue(AuthenticatedUserKey, out var user) ? user as User : null;
}
