using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Application.Internal;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;

namespace VitalTrek.API.FieldTracking.Application.ACL;

/// <summary>
///     Implementation of <see cref="IFieldTrackingContextFacade"/> backed by the Field Tracking repositories.
/// </summary>
public class FieldTrackingContextFacade(ITrackingSessionRepository trackingSessionRepository,
    IExpeditionSetupContextFacade expeditionSetupContextFacade)
    : IFieldTrackingContextFacade
{
    /// <inheritdoc />
    public async Task<bool> StageSessionEndAsync(AuthenticatedActor actor, int sessionId, DateTimeOffset endedAt,
        CancellationToken cancellationToken = default)
    {
        if (!FieldTrackingAuthorization.CanWrite(actor, actor.AgencyId)) return false;
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, actor.AgencyId, cancellationToken);
        if (session is null || !await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor,
                session.ExpeditionGroupId, cancellationToken)) return false;
        if (session.Status == SessionStatus.Completed) return true;
        if (!session.IsActive) return false;
        session.End(endedAt);
        return true;
    }

    /// <inheritdoc />
    public async Task<int?> FindActiveSessionIdByGroupIdAsync(int expeditionGroupId, CancellationToken cancellationToken = default)
    {
        var session = await trackingSessionRepository.FindActiveByGroupIdAsync(expeditionGroupId, cancellationToken);
        return session?.Id;
    }

    /// <inheritdoc />
    public async Task<bool> SessionExistsAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await trackingSessionRepository.FindByIdAsync(sessionId, cancellationToken);
        return session is not null;
    }

    /// <inheritdoc />
    public async Task<bool> SessionExistsInAgencyAsync(int agencyId, int sessionId, CancellationToken cancellationToken = default) =>
        await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, agencyId, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<bool> IsSessionActiveAsync(int sessionId, CancellationToken cancellationToken = default) =>
        (await trackingSessionRepository.FindByIdAsync(sessionId, cancellationToken))?.IsActive ?? false;

    /// <inheritdoc />
    public async Task<int?> FindExpeditionGroupIdBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        (await trackingSessionRepository.FindByIdAsync(sessionId, cancellationToken))?.ExpeditionGroupId;
}
