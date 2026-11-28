using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to request an external notification about an incident. Repeating it with the same
///     <paramref name="NotificationId"/> and content returns the existing notification instead of sending it again.
/// </summary>
/// <param name="AgencyId">The agency of the authenticated administrator.</param>
/// <param name="IncidentId">The incident identifier.</param>
/// <param name="NotificationId">The stable identity of the notification.</param>
/// <param name="RecipientType">The kind of recipient to notify.</param>
/// <param name="RecipientAddress">The e-mail address of the recipient, validated against the agency.</param>
public record SendNotificationCommand(int AgencyId, int IncidentId, Guid NotificationId,
    NotificationRecipientType RecipientType, string RecipientAddress);
