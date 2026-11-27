namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Kind of recipient notified about an incident.
/// </summary>
public enum NotificationRecipientType
{
    /// <summary>The emergency contact declared by the participant. Kept to read rows of the first version.</summary>
    EmergencyContact,

    /// <summary>An external rescue entity. Kept to read rows of the first version; real rescue services are never contacted.</summary>
    RescueEntity,

    /// <summary>A staff account of the agency (administrator or field guide) with an authorized e-mail address.</summary>
    AgencyStaff
}

/// <summary>
///     Provides conversions between <see cref="NotificationRecipientType"/> values and their external codes.
/// </summary>
public static class NotificationRecipientTypeExtensions
{
    private static readonly Dictionary<NotificationRecipientType, string> Codes = new()
    {
        [NotificationRecipientType.EmergencyContact] = "emergency_contact",
        [NotificationRecipientType.RescueEntity] = "rescue_entity",
        [NotificationRecipientType.AgencyStaff] = "agency_staff"
    };

    /// <summary>Converts the recipient type into its external snake_case code.</summary>
    public static string ToCode(this NotificationRecipientType recipientType) => Codes[recipientType];

    /// <summary>Converts an external code into a <see cref="NotificationRecipientType"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static NotificationRecipientType FromCode(string code)
    {
        foreach (var (recipientType, value) in Codes)
            if (string.Equals(value, code?.Trim(), StringComparison.OrdinalIgnoreCase)) return recipientType;
        throw new ArgumentException($"Unknown notification recipient type '{code}'.", nameof(code));
    }
}
