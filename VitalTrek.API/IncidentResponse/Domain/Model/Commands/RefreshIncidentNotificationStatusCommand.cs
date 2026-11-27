namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to ask the provider for the current state of an accepted notification. The browser never reports the
///     delivery state; only the provider answer changes it.
/// </summary>
/// <param name="AgencyId">The agency of the authenticated administrator.</param>
/// <param name="IncidentId">The incident identifier.</param>
/// <param name="NotificationId">The stable identity of the notification.</param>
public record RefreshIncidentNotificationStatusCommand(int AgencyId, int IncidentId, Guid NotificationId);
