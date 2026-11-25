using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ITelemetryRecordQueryService"/>.
/// </summary>
public class TelemetryRecordQueryService(ITelemetryRecordRepository telemetryRecordRepository,
    ITrackingSessionRepository trackingSessionRepository, IExpeditionSetupContextFacade expeditionSetupContextFacade)
    : ITelemetryRecordQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<TelemetryRecord>?> Handle(GetTelemetryRecordsBySessionIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken) =>
        await CanReadSession(query.AgencyId, query.SessionId, actor, cancellationToken)
            ? await telemetryRecordRepository.FindAllBySessionIdAsync(query.SessionId, cancellationToken)
            : null;

    /// <inheritdoc />
    public async Task<TelemetryRecord?> Handle(GetLatestTelemetryRecordByWearableIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken) =>
        await CanReadSession(query.AgencyId, query.SessionId, actor, cancellationToken)
            ? await telemetryRecordRepository.FindLatestBySessionIdAndWearableIdAsync(query.SessionId,
                query.WearableDeviceId, cancellationToken)
            : null;

    private async Task<bool> CanReadSession(int agencyId, int sessionId, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanRead(actor, agencyId)) return false;
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, agencyId, cancellationToken);
        return session is not null && await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor,
            session.ExpeditionGroupId, cancellationToken);
    }
}
