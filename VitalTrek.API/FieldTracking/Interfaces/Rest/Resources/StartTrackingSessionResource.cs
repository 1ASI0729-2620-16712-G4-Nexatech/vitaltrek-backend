using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

/// <summary>
///     Resource to start a new GPS tracking session for an expedition group.
/// </summary>
public record StartTrackingSessionResource(
    [Required(ErrorMessage = "RequiredExpeditionGroupId")] int ExpeditionGroupId);
