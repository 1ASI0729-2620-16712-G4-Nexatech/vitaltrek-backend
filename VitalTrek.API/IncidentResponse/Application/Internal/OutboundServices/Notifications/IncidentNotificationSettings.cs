namespace VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;

/// <summary>
///     Backend policy for incident notifications, bound from the IncidentNotifications configuration section. It holds no
///     provider credentials.
/// </summary>
public sealed class IncidentNotificationSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "IncidentNotifications";

    /// <summary>Environment variable with a comma separated list of allowed recipients.</summary>
    public const string AllowedRecipientsEnvironmentVariable = "NOTIFICATION_ALLOWED_RECIPIENTS";

    /// <summary>
    ///     Addresses controlled by the team that may receive notifications. The recipient must also be a staff account
    ///     of the agency.
    /// </summary>
    public List<string> AllowedRecipients { get; set; } = [];

    /// <summary>
    ///     When true (default) only <see cref="AllowedRecipients"/> are accepted, so no real emergency or rescue service
    ///     can be contacted by mistake.
    /// </summary>
    public bool RestrictToAllowedRecipients { get; set; } = true;

    /// <summary>Maximum seconds to wait for the provider before the attempt is considered uncertain.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 20;

    /// <summary>Seconds during which a started attempt blocks another attempt of the same notification.</summary>
    public int AttemptLeaseSeconds { get; set; } = 90;

    /// <summary>Gets the provider timeout.</summary>
    public TimeSpan AttemptTimeout => TimeSpan.FromSeconds(Math.Clamp(AttemptTimeoutSeconds, 1, 120));

    /// <summary>Gets the attempt lease.</summary>
    public TimeSpan AttemptLease => TimeSpan.FromSeconds(Math.Clamp(AttemptLeaseSeconds, 5, 600));

    /// <summary>Returns true when the address is in the allow list, or the allow list is not enforced.</summary>
    public bool Allows(string address) =>
        !RestrictToAllowedRecipients ||
        AllowedRecipients.Any(allowed =>
            string.Equals(allowed.Trim(), address.Trim(), StringComparison.OrdinalIgnoreCase));
}
