using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Components;

namespace VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;

/// <summary>
///     Registers the IAM request authorization middleware in the HTTP pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     Adds <see cref="RequestAuthorizationMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same application builder.</returns>
    public static IApplicationBuilder UseRequestAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestAuthorizationMiddleware>();
    }
}
