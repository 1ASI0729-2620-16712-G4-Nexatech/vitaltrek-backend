using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource to report an incident during an active tracking session. AlertId links the incident to an early warning
///     alert of the same session; omit it for a manual report.
/// </summary>
public record DeclareIncidentResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredSessionId")] int SessionId,
    [Required(ErrorMessage = "RequiredIncidentType")] string IncidentType,
    [Required(ErrorMessage = "RequiredIncidentSeverity")] string Severity,
    [Required(ErrorMessage = "RequiredIncidentDescription")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "RequiredIncidentDescription")] string Description,
    [Required(ErrorMessage = "RequiredIncidentReportedAt")] DateTimeOffset ReportedAt,
    [Range(1, int.MaxValue, ErrorMessage = "InvalidIncidentAlertId")] int? AlertId = null);
