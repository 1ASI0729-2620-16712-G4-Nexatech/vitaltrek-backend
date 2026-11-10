using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IIncidentNotificationQueryService"/>.
/// </summary>
/// <param name="notificationRepository">The incident notification repository.</param>
public class IncidentNotificationQueryService(IIncidentNotificationRepository notificationRepository)
    : IIncidentNotificationQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<IncidentNotification>> Handle(GetNotificationsByIncidentIdQuery query,
        CancellationToken cancellationToken) =>
        await notificationRepository.FindByIncidentIdAndAgencyIdAsync(query.IncidentId, query.AgencyId,
            cancellationToken);
}
