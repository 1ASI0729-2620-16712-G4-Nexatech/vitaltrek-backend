using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a field guide profile. Code is optional; the next sequential code of the agency, such as
///     GU-003, is assigned when it is omitted.
/// </summary>
public record CreateFieldGuideResource(
    [Required(ErrorMessage = "RequiredName")] string Name,
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9-]{1,19}$", ErrorMessage = "InvalidFieldGuideCode")] string? Code = null);
