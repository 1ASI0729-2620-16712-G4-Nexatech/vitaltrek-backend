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
///     Tourist manifest controller. Field guides manage the manifests of their groups and adventure tourists only see
///     their own registration.
/// </summary>
/// <param name="commandService">The manifest entry command service.</param>
/// <param name="queryService">The manifest entry query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Manifest Entries")]
[Authorize]
public class ManifestEntriesController(
    IManifestEntryCommandService commandService,
    IManifestEntryQueryService queryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<ManifestEntriesController> logger) : ControllerBase
{
    private AccessScope Scope => AccessScopeFromUserAssembler.ToAccessScopeFromUser(HttpContext.GetAuthenticatedUser()!);

    /// <summary>
    ///     Gets the manifest entries visible to the requester, optionally filtered by expedition group.
    /// </summary>
    /// <param name="expeditionGroupId">Optional expedition group identifier used as filter.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The visible manifest entries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ManifestEntryResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAllManifestEntries([FromQuery] int? expeditionGroupId, CancellationToken cancellationToken)
    {
        var entries = await queryService.Handle(new GetAllManifestEntriesQuery(Scope, expeditionGroupId), cancellationToken);
        return Ok(entries.Select(ExpeditionSetupResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets a manifest entry visible to the requester.
    /// </summary>
    /// <param name="id">The manifest entry identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The manifest entry.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ManifestEntryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetManifestEntryById(int id, CancellationToken cancellationToken)
    {
        var entry = await queryService.Handle(new GetManifestEntryByIdQuery(Scope, id), cancellationToken);
        return entry is null
            ? NotFound(localizer["ManifestEntryNotFound"].Value)
            : Ok(ExpeditionSetupResourceAssembler.ToResourceFromEntity(entry));
    }

    /// <summary>
    ///     Registers a participant in the manifest of a scheduled group and creates its empty preparation.
    /// </summary>
    /// <param name="resource">The create manifest entry resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created manifest entry.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
    [ProducesResponseType(typeof(ManifestEntryResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateManifestEntry([FromBody] CreateManifestEntryResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = ExpeditionSetupCommandFromResourceAssembler.ToCommandFromResource(resource, Scope);
            var result = await commandService.Handle(command, cancellationToken);
            return ActionResultFromExpeditionSetupResultAssembler.ToActionResult(result, this, localizer,
                ExpeditionSetupResourceAssembler.ToResourceFromEntity, entry => entry.Id, nameof(GetManifestEntryById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while registering a manifest entry in group {GroupId}",
                resource.ExpeditionGroupId);
            return BadRequest(localizer["InvalidManifestEntryRequest"].Value);
        }
    }
}
