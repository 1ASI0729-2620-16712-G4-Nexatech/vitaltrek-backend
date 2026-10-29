using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to assign the field guide of a group. Set ConfirmScheduleConflict to true to accept a guide that has another group on the same date.
/// </summary>
public record AssignFieldGuideResource(
    [Range(1, int.MaxValue, ErrorMessage = "RequiredFieldGuideId")] int FieldGuideId,
    bool ConfirmScheduleConflict = false);
