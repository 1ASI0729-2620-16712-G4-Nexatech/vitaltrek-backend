using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to change the status of a route. Only the value enabled is accepted.
/// </summary>
public record UpdateRouteStatusResource(
    [Required(ErrorMessage = "RequiredStatus")] string Status);
