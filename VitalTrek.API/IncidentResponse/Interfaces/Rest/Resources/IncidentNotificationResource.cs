namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an incident notification. Accepted means the provider took the message; only delivered
///     means the provider confirmed the delivery.
/// </summary>
public record IncidentNotificationResource(
    int Id,
    Guid NotificationId,
    int IncidentId,
    int AgencyId,
    int? RequestedByUserId,
    string RecipientType,
    string? RecipientEmail,
    string Channel,
    string Status,
    DateTimeOffset RequestedAt,
    int AttemptCount,
    DateTimeOffset? LastAttemptAt,
    string? LastErrorCode,
    string? ProviderMessageId,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? FailedAt,
    string? FailureCode);
