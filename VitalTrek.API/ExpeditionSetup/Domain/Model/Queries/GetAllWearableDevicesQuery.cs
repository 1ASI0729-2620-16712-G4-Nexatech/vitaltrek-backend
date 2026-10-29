namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the wearable inventory of an agency.
/// </summary>
public record GetAllWearableDevicesQuery(int AgencyId);
