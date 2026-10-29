using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a participant in the tourist manifest of a group.
/// </summary>
public record CreateManifestEntryResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredExpeditionGroupId")] int ExpeditionGroupId,
    [Required(ErrorMessage = "RequiredFullName")] string FullName,
    [Required(ErrorMessage = "RequiredIdentityDocument")] string IdentityDocument);
