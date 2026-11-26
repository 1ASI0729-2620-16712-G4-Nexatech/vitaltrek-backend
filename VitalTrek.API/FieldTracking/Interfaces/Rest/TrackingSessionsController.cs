using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest;

/// <summary>
///     REST controller for tracking session management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class TrackingSessionsController(
    ITrackingSessionCommandService trackingSessionCommandService,
    ITrackingSessionQueryService trackingSessionQueryService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    /// <summary>Retrieves all tracking sessions for the authenticated agency.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSessions(CancellationToken cancellationToken)
    {
        var sessions = await trackingSessionQueryService.Handle(
            new GetAllSessionsByAgencyIdQuery(Actor.AgencyId), Actor, cancellationToken);
        return Ok(sessions.Select(FieldTrackingResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>Retrieves a tracking session by its identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSessionById(int id, CancellationToken cancellationToken)
    {
        var session = await trackingSessionQueryService.Handle(
            new GetTrackingSessionByIdQuery(Actor.AgencyId, id), Actor, cancellationToken);
        if (session is null) return NotFound(localizer["TrackingSessionNotFound"].Value);
        return Ok(FieldTrackingResourceAssembler.ToResourceFromEntity(session));
    }

    /// <summary>Starts a prepared expedition (201), or returns its existing active session (200).</summary>
    [HttpPost]
    [Authorize(Role.FieldGuide)]
    [ProducesResponseType<TrackingSessionResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<TrackingSessionResource>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartSession(
        [FromBody] StartTrackingSessionResource resource, CancellationToken cancellationToken)
    {
        var command = FieldTrackingCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId);
        var result = await trackingSessionCommandService.Handle(command, Actor, cancellationToken);
        return ActionResultFromFieldTrackingResultAssembler.ToStartActionResult(result, this, localizer,
            nameof(GetSessionById));
    }
}
