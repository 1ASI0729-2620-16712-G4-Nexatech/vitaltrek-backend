using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to update a route note.
/// </summary>
public record UpdateRouteNoteResource(
    [Required(ErrorMessage = "RequiredTitle")] string Title,
    [Required(ErrorMessage = "RequiredContent")] string Content,
    [Range(1, 1000, ErrorMessage = "InvalidDisplayOrder")] int DisplayOrder);
