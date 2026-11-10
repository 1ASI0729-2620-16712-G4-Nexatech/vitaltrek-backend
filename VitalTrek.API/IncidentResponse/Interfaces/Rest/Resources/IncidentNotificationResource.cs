namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an incident notification returned to clients.
/// </summary>
public record IncidentNotificationResource(
    int Id,
    int IncidentId,
    int AgencyId,
    string RecipientType,
    DateTimeOffset SentAt,
    string Channel,
    string Status);
