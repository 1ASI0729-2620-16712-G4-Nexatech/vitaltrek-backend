namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a wearable device of the inventory.
/// </summary>
public record WearableDeviceResource(
    int Id,
    int AgencyId,
    string Code,
    string Status);
