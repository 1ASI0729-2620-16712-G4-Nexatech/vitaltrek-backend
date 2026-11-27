using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest;

/// <summary>
///     Incident notifications controller. Only operations administrators of the agency request and read external
///     communications. The delivery state changes only with the answer of the provider, never with client input.
/// </summary>
/// <param name="commandService">The incident notifications command service.</param>
/// <param name="queryService">The incident notifications query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/incidents/{incidentId:int}/notifications")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Incident Notifications")]
[Authorize(Role.OperationsAdministrator)]
public class IncidentNotificationsController(
    IIncidentNotificationCommandService commandService,
    IIncidentNotificationQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<IncidentNotificationsController> logger) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    /// <summary>
    ///     Requests an e-mail notification to an authorized staff address of the agency for an incident with a declared
    ///     emergency. The intention is persisted as pending before the provider is contacted. Repeating the request with the
    ///     same notificationId returns 200 with the existing notification and retries it only while it is pending.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="resource">The notification resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The notification with the outcome of the provider attempt.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(IncidentNotificationResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IncidentNotificationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendIncidentNotification(int incidentId,
        [FromBody] SendNotificationResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = IncidentResponseCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId,
                incidentId);
            var result = await commandService.Handle(command, Actor, cancellationToken);
            return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
                requested => IncidentResponseResourceAssembler.ToResourceFromEntity(requested.Notification),
                nameof(GetIncidentNotificationById),
                requested => new
                {
                    incidentId = requested.Notification.IncidentId,
                    notificationId = requested.Notification.NotificationId
                },
                requested => requested.Created);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while requesting a notification for incident {IncidentId}",
                incidentId);
            return ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer,
                "InvalidIncidentNotificationRequest", "IncidentNotification.InvalidData", StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    ///     Gets the notifications requested for an incident.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The notifications ordered by request time.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IncidentNotificationResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetIncidentNotifications(int incidentId, CancellationToken cancellationToken)
    {
        var notifications = await queryService.Handle(new GetNotificationsByIncidentIdQuery(Actor.AgencyId, incidentId),
            Actor, cancellationToken);
        return notifications is null
            ? ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer, "IncidentNotFound",
                "IncidentNotification.IncidentNotFound", StatusCodes.Status404NotFound)
            : Ok(notifications.Select(IncidentResponseResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets one notification of an incident by its identity.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="notificationId">The notification identity.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The notification.</returns>
    [HttpGet("{notificationId:guid}")]
    [ProducesResponseType(typeof(IncidentNotificationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetIncidentNotificationById(int incidentId, Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await queryService.Handle(
            new GetNotificationByIdQuery(Actor.AgencyId, incidentId, notificationId), Actor, cancellationToken);
        return notification is null
            ? ActionResultFromIncidentResponseResultAssembler.Problem(this, localizer, "IncidentNotificationNotFound",
                "IncidentNotification.NotFound", StatusCodes.Status404NotFound)
            : Ok(IncidentResponseResourceAssembler.ToResourceFromEntity(notification));
    }

    /// <summary>
    ///     Retries a pending notification, for example after a timeout, with the same identity and payload.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="notificationId">The notification identity.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The notification with the outcome of the new attempt.</returns>
    [HttpPost("{notificationId:guid}/attempts")]
    [ProducesResponseType(typeof(IncidentNotificationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RetryIncidentNotification(int incidentId, Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(
            new RetryIncidentNotificationCommand(Actor.AgencyId, incidentId, notificationId), Actor, cancellationToken);
        return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
            IncidentResponseResourceAssembler.ToResourceFromEntity);
    }

    /// <summary>
    ///     Asks the provider for the current state of an accepted notification and stores a confirmed delivery or failure.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="notificationId">The notification identity.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The notification with its current state.</returns>
    [HttpPost("{notificationId:guid}/status-checks")]
    [ProducesResponseType(typeof(IncidentNotificationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RefreshIncidentNotificationStatus(int incidentId, Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await commandService.Handle(
            new RefreshIncidentNotificationStatusCommand(Actor.AgencyId, incidentId, notificationId), Actor,
            cancellationToken);
        return ActionResultFromIncidentResponseResultAssembler.ToActionResult(result, this, localizer,
            IncidentResponseResourceAssembler.ToResourceFromEntity);
    }
}
