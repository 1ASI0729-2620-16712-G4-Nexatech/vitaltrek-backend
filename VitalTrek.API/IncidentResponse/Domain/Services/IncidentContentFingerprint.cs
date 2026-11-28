using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Services;

/// <summary>
///     Computes content fingerprints of client-identified Incident Response requests. A retry of the same capture always
///     produces the same value, independently of the offset used to send a timestamp or of surrounding whitespace, so a
///     reused identity with different content can be told apart from a retry.
/// </summary>
public static class IncidentContentFingerprint
{
    /// <summary>Returns the lowercase SHA-256 fingerprint of an evacuation action capture.</summary>
    public static string ForEvacuationAction(RecordEvacuationActionCommand command)
    {
        var content = new StringBuilder("evacuation-v1");
        Append(content, command.ClientActionId.ToString("N"));
        Append(content, command.IncidentId);
        Append(content, command.Description?.Trim() ?? string.Empty);
        Append(content, string.IsNullOrWhiteSpace(command.Location) ? string.Empty : command.Location.Trim());
        Append(content, Enum.IsDefined(command.Status) ? command.Status.ToCode() : string.Empty);
        Append(content, Time(command.RecordedAt));
        return Hash(content.ToString());
    }

    /// <summary>
    ///     Returns the lowercase SHA-256 fingerprint of the client part of a notification request: incident, recipient type
    ///     and address. The server-built subject and body are persisted once and reused by every retry.
    /// </summary>
    public static string ForNotification(int incidentId, NotificationRecipientType recipientType, string recipientAddress)
    {
        var content = new StringBuilder("notification-v1");
        Append(content, incidentId);
        Append(content, Enum.IsDefined(recipientType) ? recipientType.ToCode() : string.Empty);
        Append(content, recipientAddress.Trim().ToLowerInvariant());
        return Hash(content.ToString());
    }

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static void Append(StringBuilder content, object value) =>
        content.Append('|').Append(Convert.ToString(value, CultureInfo.InvariantCulture));

    // Microsecond precision, the precision of the stored capture, independent of the offset sent by the client.
    private static long Time(DateTimeOffset value) => value.UtcTicks - value.UtcTicks % 10;
}
