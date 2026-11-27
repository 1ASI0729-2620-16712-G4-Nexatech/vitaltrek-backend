using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource to execute the transition that reaches a status: confirmed, dismissed, emergency_declared, resolved or
///     closed. The legacy in_response code is read as confirmed.
/// </summary>
public record UpdateIncidentStatusResource(
    [Required(ErrorMessage = "RequiredIncidentStatus")] string Status,
    [StringLength(500, ErrorMessage = "IncidentTransitionNoteTooLong")] string? Note = null);
