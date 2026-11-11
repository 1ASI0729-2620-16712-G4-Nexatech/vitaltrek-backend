using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

/// <summary>
///     Resource to raise an early warning alert for an active tracking session.
/// </summary>
public record RaiseEarlyWarningAlertResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredSessionId")] int SessionId,
    [Range(1, int.MaxValue, ErrorMessage = "RequiredCheckpointId")] int? CheckpointId,
    [Required(ErrorMessage = "RequiredAlertType")] string AlertType,
    [Required(ErrorMessage = "RequiredSeverity")] string Severity,
    [Required(ErrorMessage = "RequiredDescription")] [MaxLength(500)] string Description);
