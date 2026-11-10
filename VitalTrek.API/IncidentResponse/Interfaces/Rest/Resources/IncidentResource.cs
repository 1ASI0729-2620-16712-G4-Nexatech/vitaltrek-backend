namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an incident returned to clients.
/// </summary>
public record IncidentResource(
    int Id,
    int SessionId,
    int AgencyId,
    DateTimeOffset ReportedAt,
    string IncidentType,
    string Severity,
    string Description,
    string Status,
    DateTimeOffset? ResolvedAt);
