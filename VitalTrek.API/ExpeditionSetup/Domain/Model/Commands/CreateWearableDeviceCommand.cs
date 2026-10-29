namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register a wearable device in the inventory of an agency.
/// </summary>
public record CreateWearableDeviceCommand(int AgencyId, string Code);
