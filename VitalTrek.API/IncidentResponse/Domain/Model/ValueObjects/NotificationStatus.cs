namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Delivery state of an incident notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>The notification was dispatched through its channel.</summary>
    Sent,

    /// <summary>The notification could not be delivered through its channel.</summary>
    Failed
}

/// <summary>
///     Provides conversions between <see cref="NotificationStatus"/> values and their external codes.
/// </summary>
public static class NotificationStatusExtensions
{
    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this NotificationStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into a <see cref="NotificationStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static NotificationStatus FromCode(string code) =>
        Enum.TryParse<NotificationStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown notification status '{code}'.", nameof(code));
}
