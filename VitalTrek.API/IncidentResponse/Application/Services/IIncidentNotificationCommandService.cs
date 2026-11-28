using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>Result of requesting a notification: the persisted notification and whether this request created it.</summary>
/// <param name="Notification">The persisted notification with the outcome of the last provider attempt.</param>
/// <param name="Created">False when the request reused an existing notification identity.</param>
public sealed record NotificationRequestResult(IncidentNotification Notification, bool Created);

/// <summary>
///     Application command service interface for incident notifications. Only operations administrators request
///     external communications.
/// </summary>
public interface IIncidentNotificationCommandService
{
    /// <summary>
    ///     Persists the notification intention, then hands it to the provider. Reusing the identity with the same content
    ///     returns the existing notification and retries it only while it is pending.
    /// </summary>
    Task<Result<NotificationRequestResult, ESendNotificationError>> Handle(SendNotificationCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Retries a pending notification with its persisted identity and payload.</summary>
    Task<Result<IncidentNotification, ESendNotificationError>> Handle(RetryIncidentNotificationCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Asks the provider for the state of an accepted notification.</summary>
    Task<Result<IncidentNotification, ESendNotificationError>> Handle(RefreshIncidentNotificationStatusCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);
}
