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
///     Field Guides controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="commandService">The command service.</param>
/// <param name="queryService">The query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Field Guides")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class FieldGuidesController(
    IFieldGuideCommandService commandService,
    IFieldGuideQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<FieldGuidesController> logger) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>
    ///     Gets the items of the agency.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The items of the agency.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FieldGuideResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetAllFieldGuides(CancellationToken cancellationToken)
    {
        var items = await queryService.Handle(new GetAllFieldGuidesQuery(AgencyId), cancellationToken);
        return Ok(items.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets an item by its identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The requested item.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FieldGuideResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetFieldGuideById(int id, CancellationToken cancellationToken)
    {
        var item = await queryService.Handle(new GetFieldGuideByIdQuery(AgencyId, id), cancellationToken);
        return item is null
            ? NotFound(localizer["FieldGuideNotFound"].Value)
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
    [ProducesResponseType(typeof(FieldGuideResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateFieldGuide([FromBody] CreateFieldGuideResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, item => item.Id, nameof(GetFieldGuideById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating Field Guides");
            return BadRequest(localizer["InvalidFieldGuideRequest"].Value);
        }
    }
}
