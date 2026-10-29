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
///     Wearable Devices controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The command service.</param>
/// <param name="queryService">The query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Wearable Devices")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class WearableDevicesController(
    IWearableDeviceCommandService commandService,
    IWearableDeviceQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<WearableDevicesController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Gets the items of the agency.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The items of the agency.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WearableDeviceResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetAllWearableDevices(CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetAllWearableDevicesQuery(AgencyId), cancellationToken);
        return Ok(items.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an item by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested item.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WearableDeviceResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetWearableDeviceById(int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetWearableDeviceByIdQuery(AgencyId, id), cancellationToken);
        return item is null
            ? NotFound(localizer["WearableDeviceNotFound"].Value)
            : Ok(ExpeditionSetupResourceAssembler.ToResourceFromEntity(item));
    }

    /// <summary>
    ///     Registers an item in the agency.
    /// </summary>
    /// <param name="resource">The creation resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created item.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(WearableDeviceResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateWearableDevice([FromBody] CreateWearableDeviceResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, item => item.Id, nameof(GetWearableDeviceById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating Wearable Devices");
            return BadRequest(localizer["InvalidWearableDeviceRequest"].Value);
        }
    }
}
