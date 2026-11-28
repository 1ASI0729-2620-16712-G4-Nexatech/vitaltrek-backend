using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;

namespace VitalTrek.API.IncidentResponse.Infrastructure.Notifications;

/// <summary>
///     Default <see cref="IIncidentNotificationSender"/> used while no external e-mail provider is registered. It is not a
///     simulation: it never contacts anybody and never claims acceptance or delivery, so every notification stays pending
///     with the provider_not_configured code until the provider adapter (task A04) replaces this registration.
/// </summary>
/// <param name="logger">Logger for diagnostics.</param>
public sealed class UnconfiguredIncidentNotificationSender(ILogger<UnconfiguredIncidentNotificationSender> logger)
    : IIncidentNotificationSender
{
    /// <inheritdoc />
    public Task<IncidentNotificationSendResult> SendAsync(IncidentNotificationMessage message,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("No incident notification provider is configured; notification {NotificationId} stays pending",
            message.NotificationId);
        return Task.FromResult(IncidentNotificationSendResult.NotConfigured());
    }

    /// <inheritdoc />
    public Task<IncidentNotificationDeliveryStatus> GetDeliveryStatusAsync(string providerMessageId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new IncidentNotificationDeliveryStatus(IncidentNotificationDeliveryOutcome.Unknown,
            "provider_not_configured"));
}
