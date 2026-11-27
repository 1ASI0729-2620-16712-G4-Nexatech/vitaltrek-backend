using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the intention to notify an authorized recipient about an incident. The intention is
///     persisted as <see cref="NotificationStatus.Pending"/> with a stable <see cref="NotificationId"/> and payload before
///     any provider is contacted. Provider acceptance moves it to accepted; only a provider confirmation moves it to
///     delivered; a definitive rejection moves it to failed. An uncertain attempt, such as a timeout, keeps it pending so
///     it can be retried with the same identity.
/// </summary>
public partial class IncidentNotification
{
    /// <summary>Channel used by the notifications of this version.</summary>
    public const string EmailChannel = "email";

    /// <summary>Maximum length allowed for the notification channel.</summary>
    public const int MaximumChannelLength = 30;

    /// <summary>Maximum length of an e-mail address.</summary>
    public const int MaximumAddressLength = 254;

    /// <summary>Maximum length of the subject.</summary>
    public const int MaximumSubjectLength = 200;

    /// <summary>Maximum length of the plain text body.</summary>
    public const int MaximumBodyLength = 2000;

    /// <summary>Maximum length of a provider or failure code.</summary>
    public const int MaximumCodeLength = 60;

    /// <summary>Maximum length of a provider message identifier.</summary>
    public const int MaximumProviderMessageIdLength = 100;

    /// <summary>Gets the database identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the stable identity of the notification, also used as the provider idempotency key.</summary>
    public Guid NotificationId { get; private set; }

    /// <summary>Gets the incident that originated the notification.</summary>
    public int IncidentId { get; private set; }

    /// <summary>Gets the agency that owns the notification.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the administrator that requested the notification, or null for rows of the first version.</summary>
    public int? RequestedByUserId { get; private set; }

    /// <summary>Gets the kind of recipient that is notified.</summary>
    public NotificationRecipientType RecipientType { get; private set; }

    /// <summary>Gets the normalized recipient address, or null for rows of the first version.</summary>
    public string? RecipientAddress { get; private set; }

    /// <summary>Gets the channel, such as email.</summary>
    public string Channel { get; private set; } = string.Empty;

    /// <summary>Gets the stable subject sent to the provider.</summary>
    public string? Subject { get; private set; }

    /// <summary>Gets the stable minimal plain text body sent to the provider. It never contains health data.</summary>
    public string? Body { get; private set; }

    /// <summary>Gets the fingerprint of the request, used to reject a reused identity with different content.</summary>
    public string? PayloadFingerprint { get; private set; }

    /// <summary>Gets the delivery state.</summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>Gets the moment the notification was requested.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Gets how many provider attempts started.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>Gets the moment the last provider attempt started.</summary>
    public DateTimeOffset? LastAttemptStartedAt { get; private set; }

    /// <summary>Gets the moment the last provider attempt finished with any outcome.</summary>
    public DateTimeOffset? LastAttemptFinishedAt { get; private set; }

    /// <summary>Gets the code of the last uncertain or transient outcome, such as provider_timeout.</summary>
    public string? LastErrorCode { get; private set; }

    /// <summary>Gets the identifier returned by the provider when it accepted the message.</summary>
    public string? ProviderMessageId { get; private set; }

    /// <summary>Gets the moment the provider accepted the message.</summary>
    public DateTimeOffset? AcceptedAt { get; private set; }

    /// <summary>Gets the moment the provider confirmed the delivery.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>Gets the moment the notification failed definitively.</summary>
    public DateTimeOffset? FailedAt { get; private set; }

    /// <summary>Gets the code of the definitive failure.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>Initializes a new instance of the <see cref="IncidentNotification"/> class for Entity Framework Core.</summary>
    public IncidentNotification()
    {
    }

    /// <summary>Persists the intention to notify an incident. No provider is contacted by the constructor.</summary>
    /// <exception cref="ArgumentException">Thrown when the notification data is invalid.</exception>
    public IncidentNotification(SendNotificationCommand command, int requestedByUserId, string subject, string body,
        string payloadFingerprint, DateTimeOffset requestedAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        IncidentId = DomainGuard.RequirePositive(command.IncidentId, nameof(command.IncidentId));
        RequestedByUserId = DomainGuard.RequirePositive(requestedByUserId, nameof(requestedByUserId));
        if (command.NotificationId == Guid.Empty) throw new ArgumentException("NotificationId is required.", nameof(command));
        if (!Enum.IsDefined(command.RecipientType))
            throw new ArgumentException("RecipientType is not a known recipient type.", nameof(command));
        NotificationId = command.NotificationId;
        RecipientType = command.RecipientType;
        RecipientAddress = NormalizeAddress(command.RecipientAddress);
        Channel = EmailChannel;
        Subject = DomainGuard.RequireText(subject, MaximumSubjectLength, nameof(subject));
        Body = DomainGuard.RequireText(body, MaximumBodyLength, nameof(body));
        PayloadFingerprint = DomainGuard.RequireText(payloadFingerprint, 64, nameof(payloadFingerprint));
        RequestedAt = requestedAt;
        Status = NotificationStatus.Pending;
    }

    /// <summary>Normalizes an e-mail address and validates its shape.</summary>
    /// <exception cref="ArgumentException">Thrown when the address is not a single plain e-mail address.</exception>
    public static string NormalizeAddress(string? address)
    {
        var value = DomainGuard.RequireText(address, MaximumAddressLength, nameof(address)).ToLowerInvariant();
        var at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1 || !value[(at + 1)..].Contains('.') ||
            value.Any(character => char.IsWhiteSpace(character) || character is ',' or ';' or '<' or '>'))
            throw new ArgumentException("Recipient address is not a valid e-mail address.", nameof(address));
        return value;
    }

    /// <summary>Returns true while an attempt started less than <paramref name="lease"/> ago and has not finished.</summary>
    public bool HasAttemptInProgress(DateTimeOffset now, TimeSpan lease) =>
        LastAttemptStartedAt is not null && LastAttemptFinishedAt is null && now - LastAttemptStartedAt < lease;

    /// <summary>Returns true when a new provider attempt may start.</summary>
    public bool CanStartAttempt(DateTimeOffset now, TimeSpan lease) =>
        Status == NotificationStatus.Pending && !HasAttemptInProgress(now, lease);

    /// <summary>Marks the start of a provider attempt. Callers persist it before contacting the provider.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the notification is not pending or an attempt is running.</exception>
    public void StartAttempt(DateTimeOffset now, TimeSpan lease)
    {
        if (!CanStartAttempt(now, lease))
            throw new InvalidOperationException("Only a pending notification without an attempt in progress can be sent.");
        AttemptCount++;
        LastAttemptStartedAt = now;
        LastAttemptFinishedAt = null;
    }

    /// <summary>The provider accepted the message. This is not a delivery confirmation.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the notification is not pending.</exception>
    public void MarkAccepted(string providerMessageId, DateTimeOffset acceptedAt)
    {
        if (Status != NotificationStatus.Pending)
            throw new InvalidOperationException("Only a pending notification can be accepted.");
        ProviderMessageId = DomainGuard.RequireText(providerMessageId, MaximumProviderMessageIdLength,
            nameof(providerMessageId));
        Status = NotificationStatus.Accepted;
        AcceptedAt = acceptedAt;
        LastErrorCode = null;
        LastAttemptFinishedAt = acceptedAt;
    }

    /// <summary>The attempt had an uncertain or transient outcome; the notification stays pending for a retry.</summary>
    public void MarkAttemptUncertain(string errorCode, DateTimeOffset finishedAt)
    {
        if (Status != NotificationStatus.Pending) return;
        LastErrorCode = Code(errorCode);
        LastAttemptFinishedAt = finishedAt;
    }

    /// <summary>The provider rejected the message definitively, or reported that a sent message bounced.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the notification was already delivered or failed.</exception>
    public void MarkFailed(string failureCode, DateTimeOffset failedAt)
    {
        if (Status is NotificationStatus.Delivered or NotificationStatus.Failed)
            throw new InvalidOperationException("A delivered or failed notification cannot fail again.");
        Status = NotificationStatus.Failed;
        FailureCode = Code(failureCode);
        FailedAt = failedAt;
        LastAttemptFinishedAt ??= failedAt;
    }

    /// <summary>The provider confirmed the delivery of an accepted message.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the notification was not accepted.</exception>
    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        if (Status != NotificationStatus.Accepted)
            throw new InvalidOperationException("Only an accepted notification can be confirmed as delivered.");
        Status = NotificationStatus.Delivered;
        DeliveredAt = deliveredAt;
    }

    private static string Code(string code) => DomainGuard.RequireText(code, MaximumCodeLength, nameof(code));
}
