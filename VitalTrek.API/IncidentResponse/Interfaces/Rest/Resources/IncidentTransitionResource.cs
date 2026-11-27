using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Optional body of an explicit incident transition: a note that justifies it.
/// </summary>
public record IncidentTransitionResource(
    [StringLength(500, ErrorMessage = "IncidentTransitionNoteTooLong")] string? Note = null);
