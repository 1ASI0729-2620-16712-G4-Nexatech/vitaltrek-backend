using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.Resources;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest;

/// <summary>
///     Checkpoints controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The checkpoints command service.</param>
/// <param name="queryService">The checkpoints query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Checkpoints")]
[Authorize]
public class CheckpointsController(
    ICheckpointCommandService commandService,
    ICheckpointQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<CheckpointsController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Gets the checkpoints of the agency, optionally filtered by route.
    /// </summary>
    /// <param name="routeId">Optional route identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The checkpoints of the agency.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CheckpointResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAllCheckpoints([FromQuery] int? routeId, CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetAllCheckpointsQuery(AgencyId, routeId), cancellationToken);
        return Ok(items.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an item by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested item.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CheckpointResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCheckpointById(int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetCheckpointByIdQuery(AgencyId, id), cancellationToken);
        return item is null
            ? NotFound(localizer["CheckpointNotFound"].Value)
            : Ok(ExpeditionSetupResourceAssembler.ToResourceFromEntity(item));
    }

    /// <summary>
    ///     Creates an item for a route of the agency.
    /// </summary>
    /// <param name="resource">The creation resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created item.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(CheckpointResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateCheckpoint([FromBody] CreateCheckpointResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, item => item.Id, nameof(GetCheckpointById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating checkpoints for route {RouteId}", resource.RouteId);
            return BadRequest(localizer["InvalidCheckpointRequest"].Value);
        }
    }
}
