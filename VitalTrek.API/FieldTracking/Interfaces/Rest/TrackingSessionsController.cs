using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest;

/// <summary>
///     REST controller for tracking session management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TrackingSessionsController(
    ITrackingSessionCommandService trackingSessionCommandService,
    ITrackingSessionQueryService trackingSessionQueryService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>Retrieves all tracking sessions for the authenticated agency.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSessions(CancellationToken cancellationToken)
    {
        var sessions = await trackingSessionQueryService.Handle(
            new GetAllSessionsByAgencyIdQuery(AgencyId), cancellationToken);
        return Ok(sessions.Select(FieldTrackingResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>Retrieves a tracking session by its identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSessionById(int id, CancellationToken cancellationToken)
    {
        var session = await trackingSessionQueryService.Handle(
            new GetTrackingSessionByIdQuery(AgencyId, id), cancellationToken);
        if (session is null) return NotFound(localizer["TrackingSessionNotFound"].Value);
        return Ok(FieldTrackingResourceAssembler.ToResourceFromEntity(session));
    }

    /// <summary>Starts a new tracking session for an expedition group.</summary>
    [HttpPost]
    public async Task<IActionResult> StartSession(
        [FromBody] StartTrackingSessionResource resource, CancellationToken cancellationToken)
    {
        var command = FieldTrackingCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId);
        var result = await trackingSessionCommandService.Handle(command, cancellationToken);
        return ActionResultFromFieldTrackingResultAssembler.ToActionResult(
            result, this, localizer,
            FieldTrackingResourceAssembler.ToResourceFromEntity,
            s => s.Id,
            nameof(GetSessionById));
    }

    /// <summary>Ends an active tracking session.</summary>
    [HttpPatch("{id:int}/end")]
    public async Task<IActionResult> EndSession(int id, CancellationToken cancellationToken)
    {
        var command = new VitalTrek.API.FieldTracking.Domain.Model.Commands.EndTrackingSessionCommand(AgencyId, id);
        var result = await trackingSessionCommandService.Handle(command, cancellationToken);
        return ActionResultFromFieldTrackingResultAssembler.ToActionResult(
            result, this, localizer,
            FieldTrackingResourceAssembler.ToResourceFromEntity,
            s => s.Id);
    }
}
