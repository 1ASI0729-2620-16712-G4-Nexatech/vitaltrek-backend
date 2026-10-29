using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.Resources;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest;

/// <summary>
///     Expedition groups controller. Administrators see every group of the agency, field guides their assigned groups
///     and adventure tourists the groups where they appear in the manifest.
/// </summary>
/// <param name="commandService">The expedition group command service.</param>
/// <param name="queryService">The expedition group query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Expedition Groups")]
[Authorize]
public class ExpeditionGroupsController(
    IExpeditionGroupCommandService commandService,
    IExpeditionGroupQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<ExpeditionGroupsController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    private AccessScope Scope => AccessScopeFromUserAssembler.ToAccessScopeFromUser(HttpContext.GetAuthenticatedUser()!);

    /// <summary>
    ///     Gets the expedition groups visible to the requester, optionally filtered by route.
    /// </summary>
    /// <param name="routeId">Optional route identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The visible expedition groups.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpeditionGroupResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAllExpeditionGroups([FromQuery] int? routeId, CancellationToken cancellationToken)
    {
        var groups = await queryService.Handle(new GetAllExpeditionGroupsQuery(Scope, routeId), cancellationToken);
        return Ok(groups.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an expedition group visible to the requester.
    /// </summary>
    /// <param name="id">The expedition group identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The expedition group.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpeditionGroupResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetExpeditionGroupById(int id, CancellationToken cancellationToken)
    {
        var group = await queryService.Handle(new GetExpeditionGroupByIdQuery(Scope, id), cancellationToken);
        return group is null
            ? NotFound(localizer["ExpeditionGroupNotFound"].Value)
            : Ok(ExpeditionSetupResourceAssembler.ToResourceFromEntity(group));
    }

    /// <summary>
    ///     Creates a scheduled expedition group for an enabled route.
    /// </summary>
    /// <param name="resource">The create expedition group resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created expedition group.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(ExpeditionGroupResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateExpeditionGroup([FromBody] CreateExpeditionGroupResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, group => group.Id, nameof(GetExpeditionGroupById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating an expedition group");
            return BadRequest(localizer["InvalidExpeditionGroupRequest"].Value);
        }
    }

    /// <summary>
    ///     Assigns the field guide responsible for a scheduled group. When the guide already has another group on the
    ///     same date the response is 409 until the request is repeated with ConfirmScheduleConflict set to true.
    /// </summary>
    /// <param name="id">The expedition group identifier.</param>
    /// <param name="resource">The field guide assignment resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated expedition group.</returns>
    [HttpPut("{id:int}/field-guide")]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(ExpeditionGroupResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AssignFieldGuide(int id, [FromBody] AssignFieldGuideResource resource,
        CancellationToken cancellationToken)
    {
        var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, id, AgencyId);
        var result = await commandService.Handle(command, cancellationToken);
        return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
            ExpeditionSetupResourceAssembler.ToResourceFromEntity, group => group.Id);
    }
}
