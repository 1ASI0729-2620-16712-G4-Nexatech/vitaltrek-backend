using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.Resources;
using VitalTrek.API.SafetyMonitoring.Application.Errors;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of the Safety Monitoring command handling.
/// </summary>
public static class ActionResultFromSafetyMonitoringResultAssembler
{
    /// <summary>
    ///     Assembles the action result of a command: 201 with a location header when <paramref name="createdAtActionName"/>
    ///     is provided, otherwise 200.
    /// </summary>
    /// <param name="result">The application result.</param>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="toResource">Function that maps the value into its resource.</param>
    /// <param name="getId">Function that obtains the identifier used in the location header.</param>
    /// <param name="createdAtActionName">The action used to build the location header.</param>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="TResource">The resource type.</typeparam>
    /// <returns>The HTTP action result.</returns>
    public static ActionResult ToActionResult<TValue, TError, TResource>(
        Result<TValue, TError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        Func<TValue, TResource> toResource,
        Func<TValue, int>? getId = null,
        string? createdAtActionName = null) where TError : Enum =>
        result switch
        {
            Result<TValue, TError>.Success success when createdAtActionName is not null && getId is not null =>
                controller.CreatedAtAction(createdAtActionName, new { id = getId(success.Value) }, toResource(success.Value)),
            Result<TValue, TError>.Success success => controller.Ok(toResource(success.Value)),
            Result<TValue, TError>.Failure failure => ToErrorResult(failure.Error, controller, localizer),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult ToErrorResult(Enum error, ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        error switch
        {
            ERaiseEarlyWarningAlertError.InvalidData =>
                controller.BadRequest(localizer["InvalidEarlyWarningAlertRequest"].Value),
            ERaiseEarlyWarningAlertError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            ERaiseEarlyWarningAlertError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            ERaiseEarlyWarningAlertError.CheckpointNotInSessionRoute =>
                controller.BadRequest(localizer["CheckpointNotInSessionRoute"].Value),
            EUpdateAlertStatusError.AlertNotFound => controller.NotFound(localizer["EarlyWarningAlertNotFound"].Value),
            EUpdateAlertStatusError.InvalidTransition =>
                controller.Conflict(localizer["EarlyWarningAlertTransitionInvalid"].Value),
            EEvaluateSessionDelaysError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            EEvaluateSessionDelaysError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult Unexpected(ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
