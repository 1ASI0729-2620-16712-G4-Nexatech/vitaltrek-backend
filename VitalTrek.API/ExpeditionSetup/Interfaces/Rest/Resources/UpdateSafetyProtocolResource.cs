using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to update a safety protocol. Status accepts active or inactive.
/// </summary>
public record UpdateSafetyProtocolResource(
    [Required(ErrorMessage = "RequiredInstructions")] string Instructions,
    [Required(ErrorMessage = "RequiredStatus")] string Status);
