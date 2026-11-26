using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Model.Queries;
using VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;
using VitalTrek.API.CheckpointSync.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest;

/// <summary>
///     Sync batches controller. Receives the readings and passages captured offline and exposes their persisted result.
///     Every operation is scoped to the agency and expedition groups of the authenticated user.
/// </summary>
/// <param name="commandService">The sync batch command service.</param>
/// <param name="queryService">The sync batch query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Sync Batches")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class SyncBatchesController(
    ISyncBatchCommandService commandService,
    ISyncBatchQueryService queryService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    /// <summary>
    ///     Receives a batch of readings and passages captured offline by the assigned field guide.
    /// </summary>
    /// <param name="resource">The batch resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The persisted batch result, also for an identical retry.</returns>
    [HttpPost]
    [Authorize(Role.FieldGuide)]
    [ProducesResponseType(typeof(SyncBatchResource), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SubmitSyncBatch([FromBody] SubmitSyncBatchResource resource,
        CancellationToken cancellationToken)
    {
        var actor = Actor;
        if (actor.FieldGuideId is null)
            return Problem(title: localizer["AccessForbidden"].Value, statusCode: StatusCodes.Status403Forbidden);
        var command = CheckpointSyncCommandFromResourceAssembler.ToCommandFromResource(resource, actor.AgencyId,
            actor.FieldGuideId.Value);
        var result = await commandService.Handle(command, actor, cancellationToken);
        return ActionResultFromCheckpointSyncResultAssembler.ToAcceptedActionResult(result, this, localizer,
            nameof(GetSyncBatchById));
    }

    /// <summary>
    ///     Gets the persisted result of a sync batch.
    /// </summary>
    /// <param name="id">The batch identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The batch result.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SyncBatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetSyncBatchById(int id, CancellationToken cancellationToken)
    {
        var actor = Actor;
        var batch = await queryService.Handle(new GetSyncBatchByIdQuery(actor.AgencyId, id), actor, cancellationToken);
        return batch is null
            ? NotFound(localizer["SyncBatchNotFound"].Value)
            : Ok(CheckpointSyncResourceAssembler.ToResourceFromEntity(batch));
    }
}
