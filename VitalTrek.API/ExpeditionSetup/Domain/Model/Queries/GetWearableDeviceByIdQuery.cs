namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a wearable device of an agency.
/// </summary>
public record GetWearableDeviceByIdQuery(int AgencyId, int WearableDeviceId);
