namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to acknowledge an active early warning alert of an agency.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="AlertId">The alert identifier.</param>
/// <param name="UserId">The authenticated user who acknowledges the alert.</param>
public record AcknowledgeAlertCommand(int AgencyId, int AlertId, int UserId);
