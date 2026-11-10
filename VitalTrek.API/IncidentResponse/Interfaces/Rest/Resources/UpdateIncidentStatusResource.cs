using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource to move an incident to another state of its lifecycle.
/// </summary>
public record UpdateIncidentStatusResource(
    [Required(ErrorMessage = "RequiredIncidentStatus")] string Status);
