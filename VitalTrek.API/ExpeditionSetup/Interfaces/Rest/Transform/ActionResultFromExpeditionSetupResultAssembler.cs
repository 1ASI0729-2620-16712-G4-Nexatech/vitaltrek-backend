using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of the Expedition Setup command handling.
/// </summary>
public static class ActionResultFromExpeditionSetupResultAssembler
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
            ECreateRouteError.InvalidData => controller.BadRequest(localizer["InvalidRouteRequest"].Value),
            ECreateRouteError.DuplicateName => controller.Conflict(localizer["RouteNameDuplicated"].Value),
            EEnableRouteError.RouteNotFound => controller.NotFound(localizer["RouteNotFound"].Value),
            EEnableRouteError.RouteWithoutCheckpoints => controller.Conflict(localizer["RouteRequiresCheckpoint"].Value),
            ECreateCheckpointError.InvalidData => controller.BadRequest(localizer["InvalidCheckpointRequest"].Value),
            ECreateCheckpointError.RouteNotFound => controller.BadRequest(localizer["RouteNotFound"].Value),
            ECreateCheckpointError.DuplicateSequenceOrder => controller.Conflict(localizer["CheckpointSequenceDuplicated"].Value),
            ECreateCheckpointError.DuplicateBluetoothId => controller.Conflict(localizer["CheckpointBluetoothIdDuplicated"].Value),
            ECreateExpectedTimeWindowError.InvalidData =>
                controller.BadRequest(localizer["ExpectedTimeWindowLimitsInvalid"].Value),
            ECreateExpectedTimeWindowError.RouteNotFound => controller.BadRequest(localizer["RouteNotFound"].Value),
            ECreateExpectedTimeWindowError.SegmentNotFound =>
                controller.BadRequest(localizer["ExpectedTimeWindowSegmentInvalid"].Value),
            ECreateExpectedTimeWindowError.DuplicateSegment =>
                controller.Conflict(localizer["ExpectedTimeWindowDuplicated"].Value),
            ERouteNoteError.InvalidData => controller.BadRequest(localizer["InvalidRouteNoteRequest"].Value),
            ERouteNoteError.RouteNotFound => controller.BadRequest(localizer["RouteNotFound"].Value),
            ERouteNoteError.RouteNoteNotFound => controller.NotFound(localizer["RouteNoteNotFound"].Value),
            ERouteNoteError.DuplicateDisplayOrder => controller.Conflict(localizer["RouteNoteDisplayOrderDuplicated"].Value),
            ESafetyProtocolError.InvalidData => controller.BadRequest(localizer["InvalidSafetyProtocolRequest"].Value),
            ESafetyProtocolError.RouteNotFound => controller.BadRequest(localizer["RouteNotFound"].Value),
            ESafetyProtocolError.SafetyProtocolNotFound => controller.NotFound(localizer["SafetyProtocolNotFound"].Value),
            ESafetyProtocolError.DuplicateProtocol => controller.Conflict(localizer["SafetyProtocolDuplicated"].Value),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult Unexpected(ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
