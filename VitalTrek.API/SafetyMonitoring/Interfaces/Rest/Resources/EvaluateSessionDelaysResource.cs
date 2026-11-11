using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

/// <summary>
///     Resource to evaluate the delays of an active tracking session.
/// </summary>
public record EvaluateSessionDelaysResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredSessionId")] int SessionId);
