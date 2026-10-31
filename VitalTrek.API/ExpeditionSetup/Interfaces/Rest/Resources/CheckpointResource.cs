namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a checkpoint returned to clients.
/// </summary>
public record CheckpointResource(
    int Id,
    int AgencyId,
    int RouteId,
    string Location,
    int SequenceOrder,
    string BluetoothId,
    double Latitude,
    double Longitude);
