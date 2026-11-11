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
using VitalTrek.API.Resources;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest;

/// <summary>
///     Checkpoint passages controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The checkpoint passages command service.</param>
/// <param name="queryService">The checkpoint passages query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Checkpoint Passages")]
[Authorize]
public class CheckpointPassagesController(
    ICheckpointPassageCommandService commandService,
    ICheckpointPassageQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<CheckpointPassagesController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Records the passage of the authenticated field guide through a checkpoint.
    /// </summary>
    /// <param name="resource">The passage resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created passage, or the existing one when it was already recorded.</returns>
    [HttpPost]
    [Authorize(Role.FieldGuide)]
    [ProducesResponseType(typeof(CheckpointPassageResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CheckpointPassageResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RecordCheckpointPassage([FromBody] RecordCheckpointPassageResource resource,
        CancellationToken cancellationToken)
    {
        var user = HttpContext.GetAuthenticatedUser()!;
        if (user.FieldGuideId is null)
            return Problem(title: localizer["AccessForbidden"].Value, statusCode: StatusCodes.Status403Forbidden);
        if (resource.PassedAt == default)
            return BadRequest(localizer["RequiredPassedAt"].Value);

        try
        {
            var command = CheckpointSyncCommandFromResourceAssembler.ToCommandFromResource(resource, user.AgencyId,
                user.FieldGuideId.Value);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromCheckpointSyncResultAssembler.ToActionResult(result, this, localizer,
                CheckpointSyncResourceAssembler.ToResourceFromEntity, item => item.Id, nameof(GetPassageById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while recording passage for session {SessionId}", resource.SessionId);
            return BadRequest(localizer["InvalidCheckpointPassageRequest"].Value);
        }
    }

    /// <summary>
    ///     Gets a checkpoint passage by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested passage.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CheckpointPassageResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetPassageById(int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetPassageByIdQuery(AgencyId, id), cancellationToken);
        return item is null
            ? NotFound(localizer["CheckpointPassageNotFound"].Value)
            : Ok(CheckpointSyncResourceAssembler.ToResourceFromEntity(item));
    }

    /// <summary>
    ///     Gets the checkpoint passages of a session or of a checkpoint. Exactly one filter is required.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier used as filter.</param>
    /// <param name="checkpointId">The checkpoint identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The matching passages ordered by passage time.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CheckpointPassageResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetPassages([FromQuery] int? sessionId, [FromQuery] int? checkpointId,
        CancellationToken cancellationToken)
    {
        if (sessionId is null == checkpointId is null)
            return BadRequest(localizer["CheckpointPassageFilterRequired"].Value);

        var items = sessionId is not null
            ? await queryService.Handle(new GetPassagesBySessionIdQuery(AgencyId, sessionId.Value), cancellationToken)
            : await queryService.Handle(new GetPassagesByCheckpointIdQuery(AgencyId, checkpointId!.Value), cancellationToken);
        return Ok(items.Select(CheckpointSyncResourceAssembler.ToResourceFromEntity));
    }
}
