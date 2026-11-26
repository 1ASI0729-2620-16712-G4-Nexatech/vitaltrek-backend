using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest;

/// <summary>Safe persisted progress; tourists receive only their own participant measurements.</summary>
[ApiController]
[Route("api/v1/tracking-sessions/{sessionId:int}/progress")]
[Authorize(Role.OperationsAdministrator, Role.FieldGuide, Role.AdventureTourist)]
public sealed class TrackingProgressController(ITrackingProgressQueryService queryService,
    IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    private AuthenticatedActor Actor => HttpContext.GetAuthenticatedActor()!;

    [HttpGet]
    public async Task<IActionResult> GetBySession(int sessionId, CancellationToken cancellationToken)
    {
        var progress = await queryService.Handle(new GetTrackingProgressBySessionIdQuery(Actor.AgencyId, sessionId),
            Actor, cancellationToken);
        return progress is null ? ProgressNotFound()
            : Ok(TrackingProgressResourceAssembler.ToResource(progress));
    }

    /// <summary>Resolves the latest session safely, including for a tourist reloading their expedition.</summary>
    [HttpGet("~/api/v1/expedition-groups/{groupId:int}/tracking-progress")]
    public async Task<IActionResult> GetByGroup(int groupId, CancellationToken cancellationToken)
    {
        var progress = await queryService.Handle(new GetTrackingProgressByGroupIdQuery(Actor.AgencyId, groupId),
            Actor, cancellationToken);
        return progress is null ? ProgressNotFound()
            : Ok(TrackingProgressResourceAssembler.ToResource(progress));
    }

    private ObjectResult ProgressNotFound() => Problem(title: localizer["TrackingProgressNotFound"].Value,
        statusCode: StatusCodes.Status404NotFound,
        extensions: new Dictionary<string, object?> { ["code"] = "Tracking.ProgressNotFound" });
}
