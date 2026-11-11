using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.Resources;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;
using VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Transform;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest;

/// <summary>
///     Early warning alerts controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The early warning alerts command service.</param>
/// <param name="queryService">The early warning alerts query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Early Warning Alerts")]
[Authorize]
public class EarlyWarningAlertsController(
    IEarlyWarningAlertCommandService commandService,
    IEarlyWarningAlertQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<EarlyWarningAlertsController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Raises an early warning alert for an active tracking session.
    /// </summary>
    /// <param name="resource">The alert resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created alert.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
    [ProducesResponseType(typeof(EarlyWarningAlertResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RaiseEarlyWarningAlert([FromBody] RaiseEarlyWarningAlertResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = SafetyMonitoringCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromSafetyMonitoringResultAssembler.ToActionResult(result, this, localizer,
                SafetyMonitoringResourceAssembler.ToResourceFromEntity, alert => alert.Id,
                nameof(GetEarlyWarningAlertById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while raising alert for session {SessionId}", resource.SessionId);
            return BadRequest(localizer["InvalidEarlyWarningAlertRequest"].Value);
        }
    }

    /// <summary>
    ///     Evaluates the delays of an active tracking session against the expected time windows of its route and
    ///     raises one delay alert per overdue segment.
    /// </summary>
    /// <param name="resource">The evaluation resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The alerts raised by this evaluation; empty when no new delay was detected.</returns>
    [HttpPost("evaluations")]
    [Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
    [ProducesResponseType(typeof(IEnumerable<EarlyWarningAlertResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> EvaluateSessionDelays([FromBody] EvaluateSessionDelaysResource resource,
        CancellationToken cancellationToken)
    {
        var command = SafetyMonitoringCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
        var result = await commandService.Handle(command, cancellationToken);
        return ActionResultFromSafetyMonitoringResultAssembler.ToActionResult(result, this, localizer,
            alerts => alerts.Select(SafetyMonitoringResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an early warning alert by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested alert.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EarlyWarningAlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEarlyWarningAlertById(int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetAlertByIdQuery(AgencyId, id), cancellationToken);
        return item is null
            ? NotFound(localizer["EarlyWarningAlertNotFound"].Value)
            : Ok(SafetyMonitoringResourceAssembler.ToResourceFromEntity(item));
    }

    /// <summary>
    ///     Gets the alerts of a tracking session when <paramref name="sessionId"/> is provided; otherwise the open
    ///     (active or acknowledged) alerts of the agency.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The matching alerts, most recent first.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EarlyWarningAlertResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetEarlyWarningAlerts([FromQuery] int? sessionId, CancellationToken cancellationToken)
    {
        var items = sessionId is not null
            ? await queryService.Handle(new GetAlertsBySessionIdQuery(AgencyId, sessionId.Value), cancellationToken)
            : await queryService.Handle(new GetActiveAlertsByAgencyIdQuery(AgencyId), cancellationToken);
        return Ok(items.Select(SafetyMonitoringResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Changes the status of an early warning alert: acknowledged (from active), dismissed (from active or
    ///     acknowledged) or resolved (from acknowledged).
    /// </summary>
    /// <param name="id">The alert identifier.</param>
    /// <param name="resource">The status resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated alert.</returns>
    [HttpPatch("{id:int}/status")]
    [Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
    [ProducesResponseType(typeof(EarlyWarningAlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateEarlyWarningAlertStatus(int id,
        [FromBody] UpdateEarlyWarningAlertStatusResource resource, CancellationToken cancellationToken)
    {
        AlertStatus status;
        try
        {
            status = AlertStatusExtensions.FromCode(resource.Status);
        }
        catch (ArgumentException)
        {
            return BadRequest(localizer["EarlyWarningAlertStatusNotSupported"].Value);
        }

        var result = status switch
        {
            AlertStatus.Acknowledged => await commandService.Handle(new AcknowledgeAlertCommand(AgencyId, id), cancellationToken),
            AlertStatus.Dismissed => await commandService.Handle(new DismissAlertCommand(AgencyId, id), cancellationToken),
            AlertStatus.Resolved => await commandService.Handle(new ResolveAlertCommand(AgencyId, id), cancellationToken),
            _ => null
        };
        if (result is null) return BadRequest(localizer["EarlyWarningAlertStatusNotSupported"].Value);

        return ActionResultFromSafetyMonitoringResultAssembler.ToActionResult(result, this, localizer,
            SafetyMonitoringResourceAssembler.ToResourceFromEntity);
    }
}
