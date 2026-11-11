using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

/// <summary>
///     Resource to change the status of an early warning alert. Accepted values: acknowledged, dismissed, resolved.
/// </summary>
public record UpdateEarlyWarningAlertStatusResource(
    [Required(ErrorMessage = "RequiredStatus")] string Status);
