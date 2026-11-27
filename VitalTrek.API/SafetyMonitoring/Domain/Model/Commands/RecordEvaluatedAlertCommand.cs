using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to record an alert produced by an automatic safety rule over persisted data.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="CheckpointId">The checkpoint related to the alert, required for delays.</param>
/// <param name="MemberId">The participant concerned, required for rules that evaluate a single participant.</param>
/// <param name="Rule">The automatic rule that produced the alert.</param>
/// <param name="Severity">The severity of the alert.</param>
/// <param name="Evidence">The evidence that supports the alert.</param>
/// <param name="EvidenceReference">
///     The reference that identifies the evidence within its rule, such as the client record identity of a reading or
///     the destination checkpoint of a delay.
/// </param>
/// <param name="Description">The description of the risk condition.</param>
public record RecordEvaluatedAlertCommand(int AgencyId, int SessionId, int? CheckpointId, int? MemberId,
    SafetyRule Rule, AlertSeverity Severity, AlertEvidence Evidence, string EvidenceReference, string Description);
