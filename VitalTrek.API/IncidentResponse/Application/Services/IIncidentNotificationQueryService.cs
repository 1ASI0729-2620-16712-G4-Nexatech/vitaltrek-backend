using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application query service interface for incident notification read operations. Only operations administrators
///     read notifications.
/// </summary>
public interface IIncidentNotificationQueryService
{
    /// <summary>Retrieves the notifications of an incident, or null when the incident is not visible.</summary>
    Task<IEnumerable<IncidentNotification>?> Handle(GetNotificationsByIncidentIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);

    /// <summary>Retrieves one notification of an incident by its identity.</summary>
    Task<IncidentNotification?> Handle(GetNotificationByIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);
}
