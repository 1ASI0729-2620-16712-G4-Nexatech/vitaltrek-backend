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
///     Participant preparations controller. Field guides assign wearables and adventure tourists register their
///     emergency contact and monitoring consent.
/// </summary>
/// <param name="commandService">The participant preparation command service.</param>
/// <param name="queryService">The participant preparation query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Participant Preparations")]
[Authorize]
public class ParticipantPreparationsController(
    IParticipantPreparationCommandService commandService,
    IParticipantPreparationQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<ParticipantPreparationsController> logger) : ControllerBase
{
    private AccessScope Scope => AccessScopeFromUserAssembler.ToAccessScopeFromUser(HttpContext.GetAuthenticatedUser()!);

    /// <summary>
    ///     Gets the participant preparations visible to the requester, optionally filtered by expedition group.
    /// </summary>
    /// <param name="expeditionGroupId">Optional expedition group identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The visible preparations.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParticipantPreparationResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAllParticipantPreparations([FromQuery] int? expeditionGroupId,
        CancellationToken cancellationToken)
    {
        var preparations = await queryService.Handle(new GetAllParticipantPreparationsQuery(Scope, expeditionGroupId),
            cancellationToken);
        return Ok(preparations.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets a participant preparation visible to the requester.
    /// </summary>
    /// <param name="id">The preparation identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The preparation.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ParticipantPreparationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetParticipantPreparationById(int id, CancellationToken cancellationToken)
    {
        var preparation = await queryService.Handle(new GetParticipantPreparationByIdQuery(Scope, id), cancellationToken);
        return preparation is null
            ? NotFound(localizer["ParticipantPreparationNotFound"].Value)
            : Ok(ExpeditionSetupResourceAssembler.ToResourceFromEntity(preparation));
    }

    /// <summary>
    ///     Assigns a wearable device to a participant of a scheduled group.
    /// </summary>
    /// <param name="id">The preparation identifier.</param>
    /// <param name="resource">The wearable assignment resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated preparation.</returns>
    [HttpPut("{id:int}/wearable-assignment")]
    [Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
    [ProducesResponseType(typeof(ParticipantPreparationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AssignWearableDevice(int id, [FromBody] AssignWearableDeviceResource resource,
        CancellationToken cancellationToken)
    {
        var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, id, Scope);
        var result = await commandService.Handle(command, cancellationToken);
        return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
            ExpeditionSetupResourceAssembler.ToResourceFromEntity, preparation => preparation.Id);
    }

    /// <summary>
    ///     Registers the emergency contact of the authenticated adventure tourist.
    /// </summary>
    /// <param name="id">The preparation identifier.</param>
    /// <param name="resource">The emergency contact resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated preparation.</returns>
    [HttpPut("{id:int}/emergency-contact")]
    [Authorize(Role.AdventureTourist)]
    [ProducesResponseType(typeof(ParticipantPreparationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RegisterEmergencyContact(int id, [FromBody] EmergencyContactResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, id, Scope);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, preparation => preparation.Id);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while registering the emergency contact of preparation {Id}", id);
            return BadRequest(localizer["InvalidParticipantPreparationRequest"].Value);
        }
    }

    /// <summary>
    ///     Grants or revokes the monitoring consent of the authenticated adventure tourist. Granting it activates the
    ///     wearable and records the date and time of the consent (Law N.° 29733).
    /// </summary>
    /// <param name="id">The preparation identifier.</param>
    /// <param name="resource">The consent resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The updated preparation.</returns>
    [HttpPut("{id:int}/consent")]
    [Authorize(Role.AdventureTourist)]
    [ProducesResponseType(typeof(ParticipantPreparationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateConsent(int id, [FromBody] UpdateConsentResource resource,
        CancellationToken cancellationToken)
    {
        var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, id, Scope);
        var result = await commandService.Handle(command, cancellationToken);
        return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
            ExpeditionSetupResourceAssembler.ToResourceFromEntity, preparation => preparation.Id);
    }
}
