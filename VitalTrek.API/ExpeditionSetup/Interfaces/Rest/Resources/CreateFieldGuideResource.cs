using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a field guide profile.
/// </summary>
public record CreateFieldGuideResource(
    [Required(ErrorMessage = "RequiredName")] string Name);
