using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;
using VitalTrek.API.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest;

/// <summary>
///     Incident notifications controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The incident notifications command service.</param>
/// <param name="queryService">The incident notifications query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/incidents/{incidentId:int}/notifications")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Incident Notifications")]
[Authorize]
public class IncidentNotificationsController(
    IIncidentNotificationCommandService commandService,
    IIncidentNotificationQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<IncidentNotificationsController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Dispatches a notification about an incident to an emergency contact or to a rescue entity.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="resource">The notification resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The dispatched notification.</returns>
    [HttpPost]
    [Authorize(Role.FieldGuide, Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(IncidentNotificationResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendIncidentNotification(int incidentId,
        [FromBody] SendNotificationResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId,
                incidentId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                IncidentResponseResourceAssembler.ToResourceFromEntity, notification => notification.Id,
                nameof(GetIncidentNotifications),
                notification => new { incidentId = notification.IncidentId });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while sending notification for incident {IncidentId}", incidentId);
            return BadRequest(localizer["InvalidIncidentNotificationRequest"].Value);
        }
    }

    /// <summary>
    ///     Gets the notifications dispatched for an incident.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The notifications ordered by dispatch time.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IncidentNotificationResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetIncidentNotifications(int incidentId, CancellationToken cancellationToken)
    {
        var notifications = await queryService.Handle(new GetNotificationsByIncidentIdQuery(AgencyId, incidentId),
            cancellationToken);
        return Ok(notifications.Select(IncidentResponseResourceAssembler.ToResourceFromEntity));
    }
}
