using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a notification dispatched to an emergency contact or to a rescue entity
///     because of an incident.
/// </summary>
public partial class IncidentNotification
{
    /// <summary>Maximum length allowed for the notification channel.</summary>
    public const int MaximumChannelLength = 30;

    /// <summary>Gets the notification identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the incident that originated the notification.</summary>
    public int IncidentId { get; private set; }

    /// <summary>Gets the agency that owns the notification.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the kind of recipient that was notified.</summary>
    public NotificationRecipientType RecipientType { get; private set; }

    /// <summary>Gets the moment the notification was dispatched.</summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>Gets the channel used to dispatch the notification, such as sms or satellite_radio.</summary>
    public string Channel { get; private set; } = string.Empty;

    /// <summary>Gets the delivery state of the notification.</summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="IncidentNotification"/> class for Entity Framework Core.
    /// </summary>
    public IncidentNotification()
    {
    }

    /// <summary>
    ///     Dispatches a new notification for an incident.
    /// </summary>
    /// <param name="command">The send notification command.</param>
    /// <param name="sentAt">The moment the notification is dispatched.</param>
    /// <exception cref="ArgumentException">Thrown when the notification data is invalid.</exception>
    public IncidentNotification(SendNotificationCommand command, DateTimeOffset sentAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        IncidentId = DomainGuard.RequirePositive(command.IncidentId, nameof(command.IncidentId));
        Channel = DomainGuard.RequireText(command.Channel, MaximumChannelLength, nameof(command.Channel))
            .ToLowerInvariant();
        if (!Enum.IsDefined(command.RecipientType))
            throw new ArgumentException("RecipientType is not a known recipient type.", nameof(command));
        RecipientType = command.RecipientType;
        SentAt = sentAt;
        Status = NotificationStatus.Sent;
    }

    /// <summary>
    ///     Marks the notification as not delivered through its channel.
    /// </summary>
    public void MarkFailed() => Status = NotificationStatus.Failed;
}
