namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an incident returned to staff clients, with its recorded transitions.
/// </summary>
public record IncidentResource(
    int Id,
    int SessionId,
    int ExpeditionGroupId,
    int AgencyId,
    int? AlertId,
    int? ReportedByUserId,
    DateTimeOffset ReportedAt,
    string IncidentType,
    string Severity,
    string Description,
    string Status,
    bool IsOpen,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? DismissedAt,
    DateTimeOffset? EmergencyDeclaredAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<IncidentStatusChangeResource> StatusChanges);
