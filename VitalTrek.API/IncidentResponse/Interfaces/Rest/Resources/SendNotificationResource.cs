using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

/// <summary>
///     Resource to dispatch a notification about an incident.
/// </summary>
public record SendNotificationResource(
    [Required(ErrorMessage = "RequiredNotificationRecipientType")] string RecipientType,
    [Required(ErrorMessage = "RequiredNotificationChannel")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "RequiredNotificationChannel")] string Channel);
