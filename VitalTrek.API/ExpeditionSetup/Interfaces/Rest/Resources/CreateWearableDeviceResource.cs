using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a wearable device.
/// </summary>
public record CreateWearableDeviceResource(
    [Required(ErrorMessage = "RequiredWearableCode")] string Code);
