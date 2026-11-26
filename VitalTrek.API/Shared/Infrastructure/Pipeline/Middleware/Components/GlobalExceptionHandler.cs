using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SharedProblemDetailsFactory = VitalTrek.API.Shared.Interfaces.Rest.ProblemDetails.ProblemDetailsFactory;

namespace VitalTrek.API.Shared.Infrastructure.Pipeline.Middleware.Components;

/// <summary>Logs unexpected failures and writes a localized problem details response.</summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    SharedProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled error processing {RequestPath}", httpContext.Request.Path);

        var problem = problemDetailsFactory.CreateUnexpectedProblemDetails(httpContext);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        if (await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            })) return true;

        // Some Accept headers have no registered writer. Keep the same safe error contract in that case.
        await httpContext.Response.WriteAsJsonAsync(problem, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
