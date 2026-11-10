using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.FieldTracking.Interfaces.ACL;

namespace VitalTrek.API.FieldTracking.Application.ACL;

/// <summary>
///     Implementation of <see cref="IFieldTrackingContextFacade"/> backed by the Field Tracking repositories.
/// </summary>
public class FieldTrackingContextFacade(ITrackingSessionRepository trackingSessionRepository)
    : IFieldTrackingContextFacade
{
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
