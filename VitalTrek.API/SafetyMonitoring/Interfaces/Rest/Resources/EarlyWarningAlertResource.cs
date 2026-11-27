namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an early warning alert returned to clients. Rule identifies the rule that produced the
///     alert and Evidence, present for evaluated alerts, keeps the original capture time of the observation. The
///     acknowledged and closed fields record who performed each transition and when.
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
    DateTimeOffset RaisedAt,
    string Rule,
    int? MemberId,
    AlertEvidenceResource? Evidence,
    int? AcknowledgedByUserId,
    DateTimeOffset? AcknowledgedAt,
    int? ClosedByUserId,
    DateTimeOffset? ClosedAt);
