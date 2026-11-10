using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application query service interface for incident notification read operations.
/// </summary>
public interface IIncidentNotificationQueryService
{
    /// <summary>Retrieves the notifications dispatched for an incident.</summary>
    Task<IEnumerable<IncidentNotification>> Handle(GetNotificationsByIncidentIdQuery query,
        CancellationToken cancellationToken);
}
