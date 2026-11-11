using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application command service interface for incident notification write operations.
/// </summary>
public interface IIncidentNotificationCommandService
{
    /// <summary>Dispatches a notification about an incident.</summary>
    Task<Result<IncidentNotification, ESendNotificationError>> Handle(SendNotificationCommand command,
        CancellationToken cancellationToken);
}
