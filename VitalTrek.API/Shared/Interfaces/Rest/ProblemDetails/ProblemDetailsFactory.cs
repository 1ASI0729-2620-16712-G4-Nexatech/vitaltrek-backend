using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.Shared.Resources;
using AspNetCoreProblemDetailsFactory = Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory;

namespace VitalTrek.API.Shared.Interfaces.Rest.ProblemDetails;

/// <summary>Builds localized HTTP errors using ASP.NET Core's standard problem details factory.</summary>
public sealed class ProblemDetailsFactory(
    AspNetCoreProblemDetailsFactory aspNetCoreFactory,
    IStringLocalizer<SharedResource> localizer)
{
    /// <summary>Preserves field validation errors and the standard trace identifier.</summary>
    public BadRequestObjectResult CreateValidationProblemDetails(ActionContext context)
    {
        var problem = aspNetCoreFactory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            statusCode: StatusCodes.Status400BadRequest,
            title: localizer["InvalidModelStateTitle"].Value);

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    /// <summary>Returns a generic server error without exposing exception messages to the client.</summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateUnexpectedProblemDetails(HttpContext context) =>
        aspNetCoreFactory.CreateProblemDetails(
            context,
            statusCode: StatusCodes.Status500InternalServerError,
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            instance: context.Request.Path);
}
