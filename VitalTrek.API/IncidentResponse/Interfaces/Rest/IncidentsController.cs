using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;
using VitalTrek.API.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest;

/// <summary>
///     Incidents controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The incidents command service.</param>
/// <param name="queryService">The incidents query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Incidents")]
[Authorize]
public class IncidentsController(
    IIncidentCommandService commandService,
    IIncidentQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<IncidentsController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Declares an incident that occurred during an active tracking session.
    /// </summary>
    /// <param name="resource">The incident resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The declared incident.</returns>
    [HttpPost]
    [Authorize(Role.FieldGuide, Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeclareIncident([FromBody] DeclareIncidentResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                IncidentResponseResourceAssembler.ToResourceFromEntity, incident => incident.Id,
                nameof(GetIncidentById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while declaring incident for session {SessionId}",
                resource.SessionId);
            return BadRequest(localizer["InvalidIncidentRequest"].Value);
        }
    }

    /// <summary>
    ///     Gets an incident by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested incident.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetIncidentById(int id, CancellationToken cancellationToken)
    {
        var incident = await queryService.Handle(new GetIncidentByIdQuery(AgencyId, id), cancellationToken);
        return incident is null
            ? NotFound(localizer["IncidentNotFound"].Value)
            : Ok(IncidentResponseResourceAssembler.ToResourceFromEntity(incident));
    }

    /// <summary>
    ///     Gets the incidents of a tracking session, or the open incidents of the agency when no session is given.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The matching incidents.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IncidentResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetIncidents([FromQuery] int? sessionId, CancellationToken cancellationToken)
    {
        var incidents = sessionId is not null
            ? await queryService.Handle(new GetIncidentsBySessionIdQuery(AgencyId, sessionId.Value), cancellationToken)
            : await queryService.Handle(new GetActiveIncidentsByAgencyIdQuery(AgencyId), cancellationToken);
        return Ok(incidents.Select(IncidentResponseResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Moves an incident to another state of its lifecycle.
    /// </summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">The resource carrying the target status.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated incident.</returns>
    [HttpPatch("{id:int}/status")]
    [Authorize(Role.FieldGuide, Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateIncidentStatus(int id, [FromBody] UpdateIncidentStatusResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId, id);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                IncidentResponseResourceAssembler.ToResourceFromEntity, incident => incident.Id);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Unknown status while updating incident {IncidentId}", id);
            return BadRequest(localizer["InvalidIncidentStatus"].Value);
        }
    }

    /// <summary>
    ///     Resolves an open incident.
    /// </summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The resolved incident.</returns>
    [HttpPatch("{id:int}/resolve")]
    [Authorize(Role.FieldGuide, Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ResolveIncident(int id, CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(new ResolveIncidentCommand(AgencyId, id), cancellationToken);
        return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
            IncidentResponseResourceAssembler.ToResourceFromEntity, incident => incident.Id);
    }
}
