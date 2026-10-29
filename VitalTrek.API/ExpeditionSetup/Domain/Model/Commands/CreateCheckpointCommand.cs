namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register a checkpoint in a route.
/// </summary>
public record CreateCheckpointCommand(
    int AgencyId,
    int RouteId,
    string Location,
    int SequenceOrder,
    string BluetoothId,
    double Latitude,
    double Longitude);
