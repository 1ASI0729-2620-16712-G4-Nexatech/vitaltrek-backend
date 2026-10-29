using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a route note.
/// </summary>
public record CreateRouteNoteResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredRouteId")] int RouteId,
    [Required(ErrorMessage = "RequiredTitle")] string Title,
    [Required(ErrorMessage = "RequiredContent")] string Content,
    [Range(1, 1000, ErrorMessage = "InvalidDisplayOrder")] int DisplayOrder);
