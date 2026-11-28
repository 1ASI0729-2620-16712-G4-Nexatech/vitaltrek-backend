namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Delivery state of an incident notification. Acceptance by the provider is not proof of delivery.
/// </summary>
public enum NotificationStatus
{
    /// <summary>The intention was persisted; the provider has not accepted it yet or the last attempt was uncertain.</summary>
    Pending,

    /// <summary>The provider accepted the message and returned its identifier. Delivery is not confirmed.</summary>
    Accepted,

    /// <summary>The provider confirmed the delivery of the message.</summary>
    Delivered,

    /// <summary>The provider rejected the message definitively. Terminal state.</summary>
    Failed
}

/// <summary>
///     Provides conversions between <see cref="NotificationStatus"/> values and their external codes.
/// </summary>
public static class NotificationStatusExtensions
{
    // The first version marked every notification as "sent" when the aggregate was built, without contacting any
    // provider. Such rows carry no evidence of delivery, so they are read as failed.
    private const string LegacySentCode = "sent";

    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this NotificationStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into a <see cref="NotificationStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static NotificationStatus FromCode(string code)
    {
        if (string.Equals(code?.Trim(), LegacySentCode, StringComparison.OrdinalIgnoreCase)) return NotificationStatus.Failed;
        return Enum.TryParse<NotificationStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown notification status '{code}'.", nameof(code));
    }
}
