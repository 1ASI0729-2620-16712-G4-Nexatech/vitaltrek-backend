using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to assign a wearable device to a participant.
/// </summary>
public record AssignWearableDeviceResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredWearableDeviceId")] int WearableDeviceId);
