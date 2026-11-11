using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of the Checkpoint Sync command handling.
/// </summary>
public static class ActionResultFromCheckpointSyncResultAssembler
{
    /// <summary>
    ///     Assembles the action result of a command that may return an existing entity: 201 when the entity was
    ///     created and 200 when an existing one was returned.
    /// </summary>
    /// <param name="result">The application result.</param>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="toResource">Function that maps the aggregate into its resource.</param>
    /// <param name="getId">Function that obtains the aggregate identifier.</param>
    /// <param name="createdAtActionName">The action used to build the location header.</param>
    /// <typeparam name="TEntity">The aggregate type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="TResource">The resource type.</typeparam>
    /// <returns>The HTTP action result.</returns>
    public static ActionResult ToActionResult<TEntity, TError, TResource>(
        Result<(TEntity Entity, bool WasCreated), TError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        Func<TEntity, TResource> toResource,
        Func<TEntity, int> getId,
        string createdAtActionName) where TError : Enum =>
        result switch
        {
            Result<(TEntity Entity, bool WasCreated), TError>.Success { Value.WasCreated: true } success =>
                controller.CreatedAtAction(createdAtActionName, new { id = getId(success.Value.Entity) },
                    toResource(success.Value.Entity)),
            Result<(TEntity Entity, bool WasCreated), TError>.Success success =>
                controller.Ok(toResource(success.Value.Entity)),
            Result<(TEntity Entity, bool WasCreated), TError>.Failure failure =>
                ToErrorResult(failure.Error, controller, localizer),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult ToErrorResult(Enum error, ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        error switch
        {
            ERecordCheckpointPassageError.InvalidData => controller.BadRequest(localizer["InvalidCheckpointPassageRequest"].Value),
            ERecordCheckpointPassageError.SessionNotFound => controller.NotFound(localizer["TrackingSessionNotFound"].Value),
            ERecordCheckpointPassageError.SessionNotActive => controller.Conflict(localizer["TrackingSessionNotActive"].Value),
            ERecordCheckpointPassageError.CheckpointNotInSessionRoute =>
                controller.BadRequest(localizer["CheckpointNotInSessionRoute"].Value),
            _ => Unexpected(controller, localizer)
        };

    private static ActionResult Unexpected(ControllerBase controller, IStringLocalizer<SharedResource> localizer) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer["UnexpectedErrorProcessingRequest"].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
