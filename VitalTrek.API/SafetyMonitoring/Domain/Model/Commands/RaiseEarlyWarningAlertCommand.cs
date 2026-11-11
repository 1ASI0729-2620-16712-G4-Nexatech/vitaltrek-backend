using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to raise an early warning alert for a tracking session.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="CheckpointId">The checkpoint related to the alert, if any.</param>
/// <param name="AlertType">The type of risk condition.</param>
/// <param name="Severity">The severity of the alert.</param>
/// <param name="Description">The description of the risk condition.</param>
public record RaiseEarlyWarningAlertCommand(int AgencyId, int SessionId, int? CheckpointId, AlertType AlertType,
    AlertSeverity Severity, string Description);
