using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ITrackingSessionQueryService"/>.
/// </summary>
public class TrackingSessionQueryService(ITrackingSessionRepository trackingSessionRepository,
    IExpeditionSetupContextFacade expeditionSetupContextFacade)
    : ITrackingSessionQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<TrackingSession>> Handle(GetAllSessionsByAgencyIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanRead(actor, query.AgencyId)) return [];
        var groupIds = (await expeditionSetupContextFacade.FindAccessibleExpeditionGroupIds(actor, cancellationToken))
            .ToHashSet();
        if (groupIds.Count == 0) return [];
        var sessions = await trackingSessionRepository.FindAllByAgencyIdAsync(query.AgencyId, cancellationToken);
        return sessions.Where(session => groupIds.Contains(session.ExpeditionGroupId)).ToList();
    }

    /// <inheritdoc />
    public async Task<TrackingSession?> Handle(GetTrackingSessionByIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanRead(actor, query.AgencyId)) return null;
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(query.SessionId, query.AgencyId,
            cancellationToken);
        return session is not null && await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor,
            session.ExpeditionGroupId, cancellationToken) ? session : null;
    }

    /// <inheritdoc />
    public async Task<TrackingSession?> Handle(GetActiveSessionByGroupIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanRead(actor, query.AgencyId) ||
            !await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor, query.ExpeditionGroupId,
                cancellationToken)) return null;
        var session = await trackingSessionRepository.FindActiveByGroupIdAsync(query.ExpeditionGroupId,
            cancellationToken);
        return session?.AgencyId == actor.AgencyId ? session : null;
    }
}
