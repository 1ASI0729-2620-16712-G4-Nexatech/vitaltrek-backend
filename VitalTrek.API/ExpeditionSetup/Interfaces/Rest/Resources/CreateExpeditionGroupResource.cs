using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to create an expedition group. DepartureDate uses the yyyy-MM-dd format.
/// </summary>
public record CreateExpeditionGroupResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredRouteId")] int RouteId,
    [Required(ErrorMessage = "RequiredName")] string Name,
    [Required(ErrorMessage = "RequiredDepartureDate")] DateOnly? DepartureDate,
    [Range(1, 100, ErrorMessage = "InvalidMaximumCapacity")] int MaximumCapacity);
