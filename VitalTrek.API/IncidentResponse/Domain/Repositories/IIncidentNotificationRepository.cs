using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="IncidentNotification"/> aggregates.
/// </summary>
public interface IIncidentNotificationRepository : IBaseRepository<IncidentNotification>
{
    /// <summary>Finds the notifications of an incident in an agency ordered by dispatch time.</summary>
    Task<IEnumerable<IncidentNotification>> FindByIncidentIdAndAgencyIdAsync(int incidentId, int agencyId,
        CancellationToken cancellationToken = default);
}
