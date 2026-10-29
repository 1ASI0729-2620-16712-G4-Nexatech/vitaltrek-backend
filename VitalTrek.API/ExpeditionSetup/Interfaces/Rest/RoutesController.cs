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
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest;

/// <summary>
///     Routes controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="routeCommandService">The route command service.</param>
/// <param name="routeQueryService">The route query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Routes")]
[Authorize]
public class RoutesController(
    IRouteCommandService routeCommandService,
    IRouteQueryService routeQueryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<RoutesController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Gets the routes of the agency.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The routes of the agency.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RouteResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAllRoutes(CancellationToken cancellationToken)
    {
        var routes = await routeQueryService.Handle(new GetAllRoutesByAgencyIdQuery(AgencyId), cancellationToken);
        return Ok(routes.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets a route by its identifier.
    /// </summary>
    /// <param name="id">The route identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The route.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RouteResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRouteById(int id, CancellationToken cancellationToken)
    {
        var route = await routeQueryService.Handle(new GetRouteByIdQuery(AgencyId, id), cancellationToken);
        return route is null
            ? NotFound(localizer["RouteNotFound"].Value)
            : Ok(ExpeditionSetupResourceAssembler.ToResourceFromEntity(route));
    }

    /// <summary>
    ///     Registers a route in draft status.
    /// </summary>
    /// <param name="resource">The create route resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created route.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(RouteResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateRoute([FromBody] CreateRouteResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await routeCommandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, route => route.Id, nameof(GetRouteById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating a route");
            return BadRequest(localizer["InvalidRouteRequest"].Value);
        }
    }

    /// <summary>
    ///     Changes the status of a route. A route can only be enabled when it has at least one checkpoint.
    /// </summary>
    /// <param name="id">The route identifier.</param>
    /// <param name="resource">The status resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated route.</returns>
    [HttpPatch("{id:int}/status")]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(RouteResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateRouteStatus(int id, [FromBody] UpdateRouteStatusResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            if (RouteStatusExtensions.FromCode(resource.Status) != RouteStatus.Enabled)
                return BadRequest(localizer["RouteStatusNotSupported"].Value);
        }
        catch (ArgumentException)
        {
            return BadRequest(localizer["RouteStatusNotSupported"].Value);
        }

        var result = await routeCommandService.Handle(new EnableRouteCommand(AgencyId, id), cancellationToken);
        return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
            ExpeditionSetupResourceAssembler.ToResourceFromEntity, route => route.Id);
    }
}
