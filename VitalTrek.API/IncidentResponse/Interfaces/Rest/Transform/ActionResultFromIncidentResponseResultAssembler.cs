using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of the Incident Response command handling.
/// </summary>
public static class ActionResultFromIncidentResponseResultAssembler
{
    /// <summary>
    ///     Assembles the action result of a command, returning 201 for creations and 200 for updates.
    /// </summary>
    /// <param name="result">The application result.</param>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="toResource">Function that maps the aggregate into its resource.</param>
    /// <param name="getId">Function that obtains the aggregate identifier.</param>
    /// <param name="createdAtActionName">The action used to build the location header, or null for updates.</param>
    /// <param name="routeValues">Route values of the location header; the identifier is used when omitted.</param>
    /// <typeparam name="TEntity">The aggregate type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="TResource">The resource type.</typeparam>
    /// <returns>The HTTP action result.</returns>
    public static ActionResult ToActionResult<TEntity, TError, TResource>(
        Result<TEntity, TError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        Func<TEntity, TResource> toResource,
        Func<TEntity, int> getId,
        string? createdAtActionName = null,
        Func<TEntity, object>? routeValues = null) where TError : Enum =>
        result switch
        {
            Result<TEntity, TError>.Success success when createdAtActionName is not null =>
                controller.CreatedAtAction(createdAtActionName,
                    routeValues is null ? new { id = getId(success.Value) } : routeValues(success.Value),
                    toResource(success.Value)),
            Result<TEntity, TError>.Success success => controller.Ok(toResource(success.Value)),
            Result<TEntity, TError>.Failure failure => ToErrorResult(failure.Error, controller, localizer),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult ToErrorResult(Enum error, ControllerBase controller,
        IStringLocalizer<SharedResource> localizer) =>
        error switch
        {
            EDeclareIncidentError.InvalidData => controller.BadRequest(localizer["InvalidIncidentRequest"].Value),
            EDeclareIncidentError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            EDeclareIncidentError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            EUpdateIncidentStatusError.InvalidData => controller.BadRequest(localizer["InvalidIncidentStatus"].Value),
            EUpdateIncidentStatusError.IncidentNotFound => controller.NotFound(localizer["IncidentNotFound"].Value),
            EUpdateIncidentStatusError.InvalidTransition =>
                controller.Conflict(localizer["IncidentStatusTransitionNotAllowed"].Value),
            EResolveIncidentError.IncidentNotFound => controller.NotFound(localizer["IncidentNotFound"].Value),
            EResolveIncidentError.InvalidTransition => controller.Conflict(localizer["IncidentNotOpen"].Value),
            ESendNotificationError.InvalidData =>
                controller.BadRequest(localizer["InvalidIncidentNotificationRequest"].Value),
            ESendNotificationError.IncidentNotFound => controller.NotFound(localizer["IncidentNotFound"].Value),
            ESendNotificationError.IncidentClosed => controller.Conflict(localizer["IncidentAlreadyClosed"].Value),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult Unexpected(ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
