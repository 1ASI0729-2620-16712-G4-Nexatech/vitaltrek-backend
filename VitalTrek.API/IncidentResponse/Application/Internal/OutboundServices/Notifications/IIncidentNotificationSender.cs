namespace VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;

/// <summary>
///     Port used by Incident Response to hand a notification to an external provider. It has no dependency on a specific
///     provider; the e-mail adapter (Resend, task A04) implements it in IncidentResponse/Infrastructure/Notifications.
///     Credentials and sender identity stay in the backend configuration of the adapter.
/// </summary>
/// <remarks>
///     Implementations must:
///     <list type="bullet">
///         <item>use <see cref="IncidentNotificationMessage.IdempotencyKey"/> as the provider idempotency key, so a
///         retry after a lost response never produces a second message;</item>
///         <item>return <see cref="IncidentNotificationSendOutcome.Accepted"/> only with the provider message identifier,
///         never claiming delivery;</item>
///         <item>return <see cref="IncidentNotificationSendOutcome.Rejected"/> only for a definitive refusal of this
///         message, for example 400 or 422;</item>
///         <item>return <see cref="IncidentNotificationSendOutcome.NotConfigured"/> for missing or invalid credentials or
///         sender, for example 401 or 403;</item>
///         <item>return <see cref="IncidentNotificationSendOutcome.Uncertain"/> for timeouts, rate limits (429), server
///         errors or network failures, and honour the cancellation token.</item>
///     </list>
/// </remarks>
public interface IIncidentNotificationSender
{
    /// <summary>Hands the message to the provider.</summary>
    /// <param name="message">The persisted message.</param>
    /// <param name="cancellationToken">Cancels the attempt; the caller applies a timeout.</param>
    /// <returns>The provider outcome.</returns>
    Task<IncidentNotificationSendResult> SendAsync(IncidentNotificationMessage message,
        CancellationToken cancellationToken);

    /// <summary>Asks the provider for the current state of an accepted message.</summary>
    /// <param name="providerMessageId">The identifier returned when the provider accepted the message.</param>
    /// <param name="cancellationToken">Cancels the query; the caller applies a timeout.</param>
    /// <returns>The state reported by the provider.</returns>
    Task<IncidentNotificationDeliveryStatus> GetDeliveryStatusAsync(string providerMessageId,
        CancellationToken cancellationToken);
}

/// <summary>Message handed to the provider. Subject and body are the stable payload persisted before the attempt.</summary>
/// <param name="NotificationId">The stable identity of the notification.</param>
/// <param name="IdempotencyKey">The provider idempotency key derived from the notification identity.</param>
/// <param name="RecipientAddress">The authorized recipient e-mail address.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Body">The minimal plain text body, without health or contact data.</param>
public sealed record IncidentNotificationMessage(Guid NotificationId, string IdempotencyKey, string RecipientAddress,
    string Subject, string Body)
{
    /// <summary>Builds the provider idempotency key of a notification identity.</summary>
    public static string IdempotencyKeyFor(Guid notificationId) => $"vitaltrek-incident-notification/{notificationId:N}";
}

/// <summary>Outcome of handing a message to the provider.</summary>
public enum IncidentNotificationSendOutcome
{
    /// <summary>The provider accepted the message and returned its identifier. Delivery is not confirmed.</summary>
    Accepted,

    /// <summary>The provider refused this message definitively.</summary>
    Rejected,

    /// <summary>The outcome is unknown or transient: timeout, rate limit, server or network error.</summary>
    Uncertain,

    /// <summary>The provider is not configured or rejected the credentials or sender.</summary>
    NotConfigured
}

/// <summary>Result of <see cref="IIncidentNotificationSender.SendAsync"/>.</summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="ProviderMessageId">The provider message identifier, required for an accepted message.</param>
/// <param name="Code">A stable code that explains a non accepted outcome, such as provider_rate_limited.</param>
public sealed record IncidentNotificationSendResult(IncidentNotificationSendOutcome Outcome,
    string? ProviderMessageId = null, string? Code = null)
{
    /// <summary>Creates an accepted result.</summary>
    public static IncidentNotificationSendResult Accepted(string providerMessageId) =>
        new(IncidentNotificationSendOutcome.Accepted, providerMessageId);

    /// <summary>Creates a definitive rejection.</summary>
    public static IncidentNotificationSendResult Rejected(string code) =>
        new(IncidentNotificationSendOutcome.Rejected, Code: code);

    /// <summary>Creates an uncertain or transient result.</summary>
    public static IncidentNotificationSendResult Uncertain(string code) =>
        new(IncidentNotificationSendOutcome.Uncertain, Code: code);

    /// <summary>Creates a result for a provider that is not configured.</summary>
    public static IncidentNotificationSendResult NotConfigured(string code = "provider_not_configured") =>
        new(IncidentNotificationSendOutcome.NotConfigured, Code: code);
}

/// <summary>Delivery state reported by the provider for an accepted message.</summary>
public enum IncidentNotificationDeliveryOutcome
{
    /// <summary>The provider confirmed the delivery.</summary>
    Delivered,

    /// <summary>The provider still reports the message as accepted, queued or sent without confirmation.</summary>
    Accepted,

    /// <summary>The provider reports that the message bounced or failed.</summary>
    Failed,

    /// <summary>The state could not be obtained.</summary>
    Unknown
}

/// <summary>Result of <see cref="IIncidentNotificationSender.GetDeliveryStatusAsync"/>.</summary>
/// <param name="Outcome">The reported state.</param>
/// <param name="Code">A stable code for a failed or unknown state.</param>
/// <param name="OccurredAt">When the provider recorded the reported state, if known.</param>
public sealed record IncidentNotificationDeliveryStatus(IncidentNotificationDeliveryOutcome Outcome, string? Code = null,
    DateTimeOffset? OccurredAt = null);
