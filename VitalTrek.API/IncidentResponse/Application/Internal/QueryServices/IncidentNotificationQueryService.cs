using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IIncidentNotificationQueryService"/>. Only operations
///     administrators of the agency read notifications, because they contain recipient addresses.
/// </summary>
/// <param name="notificationRepository">The incident notification repository.</param>
/// <param name="incidentRepository">The incident repository.</param>
public class IncidentNotificationQueryService(
    IIncidentNotificationRepository notificationRepository,
    IIncidentRepository incidentRepository) : IIncidentNotificationQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<IncidentNotification>?> Handle(GetNotificationsByIncidentIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (actor.RoleCode != IncidentActorRoles.Administrator || actor.AgencyId != query.AgencyId) return null;
        if (await incidentRepository.FindByIdAndAgencyIdAsync(query.IncidentId, query.AgencyId, cancellationToken) is null)
            return null;
        return await notificationRepository.FindByIncidentIdAndAgencyIdAsync(query.IncidentId, query.AgencyId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncidentNotification?> Handle(GetNotificationByIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (actor.RoleCode != IncidentActorRoles.Administrator || actor.AgencyId != query.AgencyId) return null;
        var notification = await notificationRepository.FindByNotificationIdAsync(query.AgencyId, query.NotificationId,
            cancellationToken);
        return notification?.IncidentId == query.IncidentId ? notification : null;
    }
}
