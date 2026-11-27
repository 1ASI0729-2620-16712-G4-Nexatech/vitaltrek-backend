namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to dismiss an open early warning alert of an agency as a false alarm.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="AlertId">The alert identifier.</param>
/// <param name="UserId">The authenticated user who dismisses the alert.</param>
public record DismissAlertCommand(int AgencyId, int AlertId, int UserId);
