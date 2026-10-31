using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing the emergency contact of a participant.
/// </summary>
public record EmergencyContactResource(
    [Required(ErrorMessage = "RequiredName")] string Name,
    [Required(ErrorMessage = "RequiredPhone")] string Phone,
    string? Relationship);
