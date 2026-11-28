using System.Globalization;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Services;

/// <summary>
///     Builds the minimal content of an incident notification. It uses only operational identifiers and codes: the free
///     text description, participant names, contacts and health information are never included, because the guide may
///     have written personal or health details in the description.
/// </summary>
public static class IncidentNotificationContent
{
    /// <summary>Prefix that identifies every message as part of the academic exercise.</summary>
    public const string SubjectPrefix = "[VitalTrek exercise]";

    /// <summary>Builds the subject and plain text body of the notification of an incident.</summary>
    public static (string Subject, string Body) Build(Incident incident)
    {
        var reference = $"INC-{incident.Id.ToString("D6", CultureInfo.InvariantCulture)}";
        var subject = $"{SubjectPrefix} {incident.Severity.ToCode()} {incident.IncidentType.ToCode()} incident {reference}";
        var body = string.Join('\n',
            $"VitalTrek incident {reference} requires the attention of the agency.",
            $"Type: {incident.IncidentType.ToCode()}",
            $"Severity: {incident.Severity.ToCode()}",
            $"Status when requested: {incident.Status.ToCode()}",
            $"Expedition group: {incident.ExpeditionGroupId.ToString(CultureInfo.InvariantCulture)}",
            $"Tracking session: {incident.SessionId.ToString(CultureInfo.InvariantCulture)}",
            $"Reported at (UTC): {incident.ReportedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}",
            string.Empty,
            "This is a test message of the VitalTrek academic demonstration. It is not a real emergency or rescue request.",
            "Personal, contact and health information are intentionally excluded. Consult the authorized VitalTrek workspace.");
        return (subject, body);
    }
}
