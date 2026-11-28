using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest;

/// <summary>
///     Incidents controller. Every operation is scoped to the agency of the authenticated user and to the groups the user
///     can see: administrators the agency, field guides their assigned groups. Adventure tourists have no access.
/// </summary>
/// <param name="commandService">The incidents command service.</param>
/// <param name="queryService">The incidents query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Incidents")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class IncidentsController(
    IIncidentCommandService commandService,
    IIncidentQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<IncidentsController> logger) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    /// <summary>
    ///     Reports an incident during an active tracking session, manually or from an early warning alert of that session.
    /// </summary>
    /// <param name="resource">The incident resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The reported incident.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeclareIncident([FromBody] DeclareIncidentResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId);
            var result = await commandService.Handle(command, Actor, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                IncidentResponseResourceAssembler.ToResourceFromEntity, nameof(GetIncidentById),
                incident => new { id = incident.Id });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while reporting incident for session {SessionId}",
                resource.SessionId);
            return ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer, "InvalidIncidentRequest",
                "Incident.InvalidData", StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    ///     Gets an incident visible to the authenticated user, with its recorded transitions.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested incident.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetIncidentById(int id, CancellationToken cancellationToken)
    {
        var incident = await queryService.Handle(new GetIncidentByIdQuery(Actor.AgencyId, id), Actor, cancellationToken);
        return incident is null
            ? ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer, "IncidentNotFound",
                "Incident.NotFound", StatusCodes.Status404NotFound)
            : Ok(IncidentResponseResourceAssembler.ToResourceFromEntity(incident));
    }

    /// <summary>
    ///     Gets the incidents of a tracking session, or the open incidents of the visible groups when no session is given.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The matching incidents.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IncidentResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetIncidents([FromQuery] int? sessionId, CancellationToken cancellationToken)
    {
        var incidents = sessionId is not null
            ? await queryService.Handle(new GetIncidentsBySessionIdQuery(Actor.AgencyId, sessionId.Value), Actor,
                cancellationToken)
            : await queryService.Handle(new GetActiveIncidentsByAgencyIdQuery(Actor.AgencyId), Actor, cancellationToken);
        return Ok(incidents.Select(IncidentResponseResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Executes the transition that reaches the given status. Kept for clients of the first version; the explicit
    ///     transition endpoints are preferred. The legacy in_response code confirms the incident.
    /// </summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">The resource carrying the target status.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated incident.</returns>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateIncidentStatus(int id, [FromBody] UpdateIncidentStatusResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId, id);
            var result = await commandService.Handle(command, Actor, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                IncidentResponseResourceAssembler.ToResourceFromEntity);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Unknown status while updating incident {IncidentId}", id);
            return ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer, "InvalidIncidentStatus",
                "Incident.InvalidStatus", StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>The assigned field guide confirms a reported incident after verifying it in the field.</summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">Optional note.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The confirmed incident.</returns>
    [HttpPatch("{id:int}/confirm")]
    [Authorize(Role.FieldGuide)]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult> ConfirmIncident(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IncidentTransitionResource? resource,
        CancellationToken cancellationToken) =>
        Transition(id, resource, IncidentStatus.Confirmed, cancellationToken);

    /// <summary>The assigned field guide dismisses a reported incident as a false report.</summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">Optional note.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The dismissed incident.</returns>
    [HttpPatch("{id:int}/dismiss")]
    [Authorize(Role.FieldGuide)]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult> DismissIncident(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IncidentTransitionResource? resource,
        CancellationToken cancellationToken) =>
        Transition(id, resource, IncidentStatus.Dismissed, cancellationToken);

    /// <summary>An operations administrator declares an emergency for a confirmed incident.</summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">Optional note.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The incident with the declared emergency.</returns>
    [HttpPatch("{id:int}/declare-emergency")]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult> DeclareIncidentEmergency(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IncidentTransitionResource? resource,
        CancellationToken cancellationToken) =>
        Transition(id, resource, IncidentStatus.EmergencyDeclared, cancellationToken);

    /// <summary>Resolves a confirmed incident or a declared emergency.</summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">Optional note.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The resolved incident.</returns>
    [HttpPatch("{id:int}/resolve")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult> ResolveIncident(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IncidentTransitionResource? resource,
        CancellationToken cancellationToken) =>
        Transition(id, resource, IncidentStatus.Resolved, cancellationToken);

    /// <summary>Closes a resolved incident. A closed incident admits no further changes.</summary>
    /// <param name="id">The incident identifier.</param>
    /// <param name="resource">Optional note.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The closed incident.</returns>
    [HttpPatch("{id:int}/close")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult> CloseIncident(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IncidentTransitionResource? resource,
        CancellationToken cancellationToken) =>
        Transition(id, resource, IncidentStatus.Closed, cancellationToken);

    private async Task<ActionResult> Transition(int id, IncidentTransitionResource? resource, IncidentStatus target,
        CancellationToken cancellationToken)
    {
        var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId, id,
            target);
        var result = await commandService.Handle(command, Actor, cancellationToken);
        return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
            IncidentResponseResourceAssembler.ToResourceFromEntity);
    }
}
