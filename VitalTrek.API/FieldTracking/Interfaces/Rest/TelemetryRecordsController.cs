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
///     REST controller for telemetry record submission and retrieval.
/// </summary>
[ApiController]
[Route("api/v1/tracking-sessions/{sessionId:int}/[controller]")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide)]
public class TelemetryRecordsController(
    ITelemetryRecordCommandService telemetryRecordCommandService,
    ITelemetryRecordQueryService telemetryRecordQueryService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    /// <summary>Retrieves all telemetry records for a session.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRecordsBySession(int sessionId, CancellationToken cancellationToken)
    {
        var records = await telemetryRecordQueryService.Handle(
            new GetTelemetryRecordsBySessionIdQuery(Actor.AgencyId, sessionId), Actor, cancellationToken);
        if (records is null) return NotFound(localizer["TrackingSessionNotFound"].Value);
        return Ok(records.Select(FieldTrackingResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>Submits captures with stable client identities. Identical retries return the original readings.</summary>
    [HttpPost]
    [Authorize(Role.FieldGuide)]
    public async Task<IActionResult> SubmitBatch(
        int sessionId,
        [FromBody] SubmitTelemetryBatchResource resource,
        CancellationToken cancellationToken)
    {
        if (resource.Records.Any(entry => entry is null))
            return BadRequest(localizer["InvalidTelemetryBatchRequest"].Value);
        var command = FieldTrackingCommandFromResourceAssembler.ToCommandFromResource(resource, Actor.AgencyId, sessionId);
        var result = await telemetryRecordCommandService.Handle(command, Actor, cancellationToken);
        return ActionResultFromFieldTrackingResultAssembler.ToActionResult(
            result, this, localizer,
            records => records.Select(FieldTrackingResourceAssembler.ToResourceFromEntity).ToList(),
            _ => sessionId);
    }
}
