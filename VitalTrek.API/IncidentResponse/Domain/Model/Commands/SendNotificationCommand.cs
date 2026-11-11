using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to dispatch a notification about an incident.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="IncidentId">The incident identifier.</param>
/// <param name="RecipientType">The kind of recipient to notify.</param>
/// <param name="Channel">The channel used to dispatch the notification.</param>
public record SendNotificationCommand(int AgencyId, int IncidentId, NotificationRecipientType RecipientType,
    string Channel);
