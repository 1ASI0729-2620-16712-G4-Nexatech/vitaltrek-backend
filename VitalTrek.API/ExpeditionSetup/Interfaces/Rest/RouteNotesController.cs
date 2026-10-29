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
///     Route Notes controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The route notes command service.</param>
/// <param name="queryService">The route notes query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Route Notes")]
[Authorize]
public class RouteNotesController(
    IRouteNoteCommandService commandService,
    IRouteNoteQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<RouteNotesController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Gets the route notes of the agency, optionally filtered by route.
    /// </summary>
    /// <param name="routeId">Optional route identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The route notes of the agency.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RouteNoteResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAllRouteNotes([FromQuery] int? routeId, CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetAllRouteNotesQuery(AgencyId, routeId), cancellationToken);
        return Ok(items.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an item by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested item.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RouteNoteResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRouteNoteById(int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetRouteNoteByIdQuery(AgencyId, id), cancellationToken);
        return item is null
            ? NotFound(localizer["RouteNoteNotFound"].Value)
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
    [ProducesResponseType(typeof(RouteNoteResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateRouteNote([FromBody] CreateRouteNoteResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, item => item.Id, nameof(GetRouteNoteById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating route notes for route {RouteId}", resource.RouteId);
            return BadRequest(localizer["InvalidRouteNoteRequest"].Value);
        }
    }

    /// <summary>
    ///     Updates an item of the agency.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="resource">The update resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated item.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(RouteNoteResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateRouteNote(int id, [FromBody] UpdateRouteNoteResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, id, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, item => item.Id);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while updating route notes {Id}", id);
            return BadRequest(localizer["InvalidRouteNoteRequest"].Value);
        }
    }
}
