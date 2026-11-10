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
///     REST controller for telemetry record submission and retrieval.
/// </summary>
[ApiController]
[Route("api/v1/tracking-sessions/{sessionId:int}/[controller]")]
[Authorize]
public class TelemetryRecordsController(
    ITelemetryRecordCommandService telemetryRecordCommandService,
    ITelemetryRecordQueryService telemetryRecordQueryService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    private int AgencyId => HttpContext.GetAuthenticatedUser()!.AgencyId;

    /// <summary>Retrieves all telemetry records for a session.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRecordsBySession(int sessionId, CancellationToken cancellationToken)
    {
        var records = await telemetryRecordQueryService.Handle(
            new GetTelemetryRecordsBySessionIdQuery(AgencyId, sessionId), cancellationToken);
        return Ok(records.Select(FieldTrackingResourceAssembler.ToResourceFromEntity));
    }

    /// <summary>Submits a batch of GPS measurements for a wearable device.</summary>
    [HttpPost]
    public async Task<IActionResult> SubmitBatch(
        int sessionId,
        [FromBody] SubmitTelemetryBatchResource resource,
        CancellationToken cancellationToken)
    {
        var command = FieldTrackingCommandFromResourceAssembler.ToCommandFromResource(resource, AgencyId, sessionId);
        var result = await telemetryRecordCommandService.Handle(command, cancellationToken);
        return ActionResultFromFieldTrackingResultAssembler.ToActionResult(
            result, this, localizer,
            records => records.Select(FieldTrackingResourceAssembler.ToResourceFromEntity).ToList(),
            _ => sessionId);
    }
}
