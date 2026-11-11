namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an early warning alert returned to clients.
/// </summary>
public record EarlyWarningAlertResource(
    int Id,
    int SessionId,
    int ExpeditionGroupId,
    int AgencyId,
    int? CheckpointId,
    string AlertType,
    string Severity,
    string Status,
    string Description,
    DateTimeOffset RaisedAt);
