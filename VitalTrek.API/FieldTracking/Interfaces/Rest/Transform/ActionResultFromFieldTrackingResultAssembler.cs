using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of Field Tracking command handling.
/// </summary>
public static class ActionResultFromFieldTrackingResultAssembler
{
    /// <summary>
    ///     Assembles the action result of a command, returning 201 for creations and 200 for updates.
    /// </summary>
    public static ActionResult ToActionResult<TEntity, TError, TResource>(
        Result<TEntity, TError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        Func<TEntity, TResource> toResource,
        Func<TEntity, int> getId,
        string? createdAtActionName = null) where TError : Enum =>
        result switch
        {
            Result<TEntity, TError>.Success success when createdAtActionName is not null =>
                controller.CreatedAtAction(createdAtActionName, new { id = getId(success.Value) }, toResource(success.Value)),
            Result<TEntity, TError>.Success success => controller.Ok(toResource(success.Value)),
            Result<TEntity, TError>.Failure failure => ToErrorResult(failure.Error, controller, localizer),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult ToErrorResult(Enum error, ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        error switch
        {
            EStartTrackingSessionError.InvalidData => controller.BadRequest(localizer["InvalidTrackingSessionRequest"].Value),
            EStartTrackingSessionError.SessionAlreadyActive => controller.Conflict(localizer["TrackingSessionAlreadyActive"].Value),
            EStartTrackingSessionError.UnexpectedError => Unexpected(controller, localizer),
            EEndTrackingSessionError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            EEndTrackingSessionError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            EEndTrackingSessionError.UnexpectedError => Unexpected(controller, localizer),
            ESubmitTelemetryBatchError.InvalidData => controller.BadRequest(localizer["InvalidTelemetryBatchRequest"].Value),
            ESubmitTelemetryBatchError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            ESubmitTelemetryBatchError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            ESubmitTelemetryBatchError.UnexpectedError => Unexpected(controller, localizer),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult Unexpected(ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
