using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.Shared.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.FieldTracking.Application.Model;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of Field Tracking command handling.
/// </summary>
public static class ActionResultFromFieldTrackingResultAssembler
{
    public static ActionResult ToStartActionResult(Result<TrackingSessionStartResult, EStartTrackingSessionError> result,
        ControllerBase controller, IStringLocalizer<SharedResource> localizer, string actionName) => result switch
    {
        Result<TrackingSessionStartResult, EStartTrackingSessionError>.Success { Value.WasCreated: true } success =>
            controller.CreatedAtAction(actionName, new { id = success.Value.Session.Id },
                FieldTrackingResourceAssembler.ToResourceFromEntity(success.Value.Session)),
        Result<TrackingSessionStartResult, EStartTrackingSessionError>.Success success =>
            controller.Ok(FieldTrackingResourceAssembler.ToResourceFromEntity(success.Value.Session)),
        Result<TrackingSessionStartResult, EStartTrackingSessionError>.Failure failure =>
            ToErrorResult(failure.Error, controller, localizer),
        _ => Unexpected(controller, localizer)
    };

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
            EStartTrackingSessionError.AccessForbidden or
                ESubmitTelemetryBatchError.AccessForbidden => controller.Problem(
                    title: localizer["AccessForbidden"].Value, statusCode: StatusCodes.Status403Forbidden),
            EStartTrackingSessionError.GroupNotFound => controller.NotFound(localizer["ExpeditionGroupNotFound"].Value),
            EStartTrackingSessionError.InvalidData => controller.BadRequest(localizer["InvalidTrackingSessionRequest"].Value),
            EStartTrackingSessionError.GroupNotScheduled => controller.Conflict(localizer["TrackingGroupNotScheduled"].Value),
            EStartTrackingSessionError.RouteNotReady => controller.Conflict(localizer["TrackingRouteNotReady"].Value),
            EStartTrackingSessionError.ProtocolNotReady => controller.Conflict(localizer["TrackingProtocolNotReady"].Value),
            EStartTrackingSessionError.ParticipantsNotReady => controller.Conflict(localizer["TrackingParticipantsNotReady"].Value),
            EStartTrackingSessionError.ConcurrentChange => controller.Conflict(localizer["TrackingConcurrentChange"].Value),
            EStartTrackingSessionError.UnexpectedError => Unexpected(controller, localizer),
            ESubmitTelemetryBatchError.InvalidData => TelemetryError(controller, localizer,
                "InvalidTelemetryBatchRequest", "Telemetry.InvalidData", StatusCodes.Status400BadRequest),
            ESubmitTelemetryBatchError.WearableNotAssigned => TelemetryError(controller, localizer,
                "TelemetryWearableNotAssigned", "Telemetry.WearableNotAssigned", StatusCodes.Status404NotFound),
            ESubmitTelemetryBatchError.ConsentRequired => TelemetryError(controller, localizer,
                "TelemetryConsentRequired", "Telemetry.ConsentRequired", StatusCodes.Status409Conflict),
            ESubmitTelemetryBatchError.RecordIdentityConflict => TelemetryError(controller, localizer,
                "TelemetryIdentityConflict", "Telemetry.RecordIdentityConflict", StatusCodes.Status409Conflict),
            ESubmitTelemetryBatchError.ConcurrentChange => TelemetryError(controller, localizer,
                "TelemetryConcurrentChange", "Telemetry.ConcurrentChange", StatusCodes.Status409Conflict),
            ESubmitTelemetryBatchError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            ESubmitTelemetryBatchError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            ESubmitTelemetryBatchError.UnexpectedError => Unexpected(controller, localizer),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult TelemetryError(ControllerBase controller, IStringLocalizer<SharedResource> localizer,
        string key, string code, int statusCode) => controller.Problem(title: localizer[key].Value,
            statusCode: statusCode, extensions: new Dictionary<string, object?> { ["code"] = code });

    private static ActionResult Unexpected(ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
