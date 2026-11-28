using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest;

/// <summary>
///     Evacuation actions of an incident. The assigned field guide records them, possibly from an offline queue; staff of
///     the group read them. Adventure tourists have no access.
/// </summary>
/// <param name="commandService">The evacuation action command service.</param>
/// <param name="queryService">The evacuation action query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/incidents/{incidentId:int}/evacuation-actions")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Evacuation Actions")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class EvacuationActionsController(
    IEvacuationActionCommandService commandService,
    IEvacuationActionQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<EvacuationActionsController> logger) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    /// <summary>
    ///     Records an evacuation action captured during a declared emergency. A retry with the same clientActionId and
    ///     content returns 200 with the first record; a reused clientActionId with different content returns 409.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="resource">The captured action.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The persisted action.</returns>
    [HttpPost]
    [Authorize(Role.FieldGuide)]
    [ProducesResponseType(typeof(EvacuationActionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(EvacuationActionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RecordEvacuationAction(int incidentId,
        [FromBody] RecordEvacuationActionResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId,
                incidentId);
            var result = await commandService.Handle(command, Actor, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                recorded => IncidentResponseResourceAssembler.ToResourceFromEntity(recorded.Action),
                nameof(GetEvacuationActions), recorded => new { incidentId = recorded.Action.IncidentId },
                recorded => recorded.Created);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while recording an evacuation action of incident {IncidentId}",
                incidentId);
            return ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer,
                "InvalidEvacuationActionRequest", "EvacuationAction.InvalidData", StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    ///     Gets the evacuation actions of an incident ordered by capture time.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The actions of the incident.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EvacuationActionResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEvacuationActions(int incidentId, CancellationToken cancellationToken)
    {
        var actions = await queryService.Handle(new GetEvacuationActionsByIncidentIdQuery(Actor.AgencyId, incidentId),
            Actor, cancellationToken);
        return actions is null
            ? ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer, "IncidentNotFound",
                "EvacuationAction.IncidentNotFound", StatusCodes.Status404NotFound)
            : Ok(actions.Select(IncidentResponseResourceAssembler.ToResourceFromEntity));
    }
}
