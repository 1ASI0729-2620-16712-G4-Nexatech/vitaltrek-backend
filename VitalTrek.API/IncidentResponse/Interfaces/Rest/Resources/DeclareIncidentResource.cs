using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource to declare an incident during an active tracking session.
/// </summary>
public record DeclareIncidentResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredSessionId")] int SessionId,
    [Required(ErrorMessage = "RequiredIncidentType")] string IncidentType,
    [Required(ErrorMessage = "RequiredIncidentSeverity")] string Severity,
    [Required(ErrorMessage = "RequiredIncidentDescription")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "RequiredIncidentDescription")] string Description,
    [Required(ErrorMessage = "RequiredIncidentReportedAt")] DateTimeOffset ReportedAt);
