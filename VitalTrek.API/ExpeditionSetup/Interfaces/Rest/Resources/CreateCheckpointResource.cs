using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a checkpoint in a route.
/// </summary>
public record CreateCheckpointResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredRouteId")] int RouteId,
    [Required(ErrorMessage = "RequiredLocation")] string Location,
    [Range(1, 1000, ErrorMessage = "InvalidSequenceOrder")] int SequenceOrder,
    [Required(ErrorMessage = "RequiredBluetoothId")] string BluetoothId,
    [Range(-90, 90, ErrorMessage = "InvalidLatitude")] double Latitude,
    [Range(-180, 180, ErrorMessage = "InvalidLongitude")] double Longitude);
