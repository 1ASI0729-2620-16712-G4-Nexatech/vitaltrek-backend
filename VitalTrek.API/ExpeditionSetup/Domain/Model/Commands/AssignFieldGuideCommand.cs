namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to assign the field guide responsible for an expedition group. A schedule conflict requires explicit confirmation.
/// </summary>
public record AssignFieldGuideCommand(int AgencyId, int ExpeditionGroupId, int FieldGuideId, bool ConfirmScheduleConflict);
