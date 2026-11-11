namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to resolve an acknowledged early warning alert of an agency.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="AlertId">The alert identifier.</param>
public record ResolveAlertCommand(int AgencyId, int AlertId);
