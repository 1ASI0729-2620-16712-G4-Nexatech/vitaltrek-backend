using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to configure the safety protocol of a route.
/// </summary>
public record CreateSafetyProtocolResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredRouteId")] int RouteId,
    [Required(ErrorMessage = "RequiredInstructions")] string Instructions);
