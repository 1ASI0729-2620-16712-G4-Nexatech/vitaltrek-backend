namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to retry a pending notification with its persisted identity and payload, for example after a timeout.
/// </summary>
/// <param name="AgencyId">The agency of the authenticated administrator.</param>
/// <param name="IncidentId">The incident identifier.</param>
/// <param name="NotificationId">The stable identity of the notification.</param>
public record RetryIncidentNotificationCommand(int AgencyId, int IncidentId, Guid NotificationId);
