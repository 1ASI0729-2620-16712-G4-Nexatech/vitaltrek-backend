using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing an early warning alert raised when a risk condition is detected during a
///     tracking session.
/// </summary>
public partial class EarlyWarningAlert
{
    /// <summary>Maximum length of the alert description.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Maximum length of the reference that identifies the evidence within its rule.</summary>
    public const int EvidenceReferenceMaxLength = 80;

    /// <summary>Maximum length of the evidence identity.</summary>
    public const int EvidenceKeyMaxLength = 120;

    /// <summary>Gets the alert identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the tracking session the alert belongs to.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the expedition group tracked by the session.</summary>
    public int ExpeditionGroupId { get; private set; }

    /// <summary>Gets the agency that owns the alert.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the checkpoint related to the alert, if any.</summary>
    public int? CheckpointId { get; private set; }

    /// <summary>Gets the type of risk condition.</summary>
    public AlertType AlertType { get; private set; }

    /// <summary>Gets the severity of the alert.</summary>
    public AlertSeverity Severity { get; private set; }

    /// <summary>Gets the moment the alert was raised.</summary>
    public DateTimeOffset RaisedAt { get; private set; }

    /// <summary>Gets the lifecycle status of the alert.</summary>
    public AlertStatus Status { get; private set; }

    /// <summary>Gets the description of the risk condition.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the participant concerned by the alert, when the rule evaluates a single participant.</summary>
    public int? MemberId { get; private set; }

    /// <summary>Gets the rule that produced the alert.</summary>
    public SafetyRule Rule { get; private set; }

    /// <summary>
    ///     Gets the identity of the evidence within the session. The same evidence never produces a second alert, so a
    ///     repeated evaluation cannot duplicate or reopen it.
    /// </summary>
    public string EvidenceKey { get; private set; } = string.Empty;

    /// <summary>Gets the observed value of the evidence, if the alert was evaluated.</summary>
    public decimal? EvidenceObservedValue { get; private set; }

    /// <summary>Gets the lowest expected value of the evidence, if any.</summary>
    public decimal? EvidenceExpectedMinimum { get; private set; }

    /// <summary>Gets the highest expected value of the evidence, if any.</summary>
    public decimal? EvidenceExpectedMaximum { get; private set; }

    /// <summary>Gets the unit of the evidence values, if the alert was evaluated.</summary>
    public string? EvidenceUnit { get; private set; }

    /// <summary>Gets the original moment of the observation, if the alert was evaluated.</summary>
    public DateTimeOffset? EvidenceRecordedAt { get; private set; }

    /// <summary>Gets the client identity of the reading or passage that supports the evidence, if any.</summary>
    public Guid? EvidenceSourceRecordId { get; private set; }

    /// <summary>Gets whether the evidence comes from the academic simulation.</summary>
    public bool EvidenceSimulated { get; private set; }

    /// <summary>Gets the user who acknowledged the alert, if it was acknowledged.</summary>
    public int? AcknowledgedByUserId { get; private set; }

    /// <summary>Gets the moment the alert was acknowledged, if it was acknowledged.</summary>
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    /// <summary>Gets the user who closed the alert by resolving or dismissing it, if it was closed.</summary>
    public int? ClosedByUserId { get; private set; }

    /// <summary>Gets the moment the alert was resolved or dismissed, if it was closed.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EarlyWarningAlert"/> class for Entity Framework Core.
    /// </summary>
    public EarlyWarningAlert()
    {
    }

    /// <summary>
    ///     Initializes a new active early warning alert.
    /// </summary>
    /// <param name="command">The raise early warning alert command.</param>
    /// <param name="expeditionGroupId">The expedition group tracked by the session.</param>
    /// <param name="raisedAt">The moment the alert is raised.</param>
    /// <exception cref="ArgumentException">Thrown when the alert data is invalid.</exception>
    public EarlyWarningAlert(RaiseEarlyWarningAlertCommand command, int expeditionGroupId, DateTimeOffset raisedAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        ExpeditionGroupId = DomainGuard.RequirePositive(expeditionGroupId, nameof(expeditionGroupId));
        if (command.CheckpointId is not null)
            CheckpointId = DomainGuard.RequirePositive(command.CheckpointId.Value, nameof(command.CheckpointId));
        if (!Enum.IsDefined(command.AlertType))
            throw new ArgumentException("Unknown alert type.", nameof(command));
        if (!Enum.IsDefined(command.Severity))
            throw new ArgumentException("Unknown alert severity.", nameof(command));
        AlertType = command.AlertType;
        Severity = command.Severity;
        Description = DomainGuard.RequireText(command.Description, DescriptionMaxLength, nameof(command.Description));
        RaisedAt = raisedAt;
        Status = AlertStatus.Active;
        Rule = SafetyRule.Manual;
        // Every manual report is a distinct evidence: it never deduplicates against another report.
        EvidenceKey = EvidenceKeyFor(SafetyRule.Manual, Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    ///     Initializes a new active alert produced by an automatic safety rule over persisted data.
    /// </summary>
    /// <param name="command">The record evaluated alert command.</param>
    /// <param name="expeditionGroupId">The expedition group tracked by the session.</param>
    /// <param name="raisedAt">The moment the alert is raised by the evaluation.</param>
    /// <exception cref="ArgumentException">Thrown when the alert data is invalid or does not match its rule.</exception>
    public EarlyWarningAlert(RecordEvaluatedAlertCommand command, int expeditionGroupId, DateTimeOffset raisedAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        ExpeditionGroupId = DomainGuard.RequirePositive(expeditionGroupId, nameof(expeditionGroupId));
        if (command.CheckpointId is not null)
            CheckpointId = DomainGuard.RequirePositive(command.CheckpointId.Value, nameof(command.CheckpointId));
        if (command.MemberId is not null)
            MemberId = DomainGuard.RequirePositive(command.MemberId.Value, nameof(command.MemberId));
        if (!Enum.IsDefined(command.Rule) || command.Rule == SafetyRule.Manual)
            throw new ArgumentException("Only an automatic safety rule records an evaluated alert.", nameof(command));
        if (!Enum.IsDefined(command.Severity))
            throw new ArgumentException("Unknown alert severity.", nameof(command));
        ArgumentNullException.ThrowIfNull(command.Evidence, nameof(command.Evidence));
        RequireRuleScope(command.Rule, CheckpointId, MemberId, command.Evidence);
        Rule = command.Rule;
        AlertType = command.Rule.ToAlertType();
        Severity = command.Severity;
        EvidenceKey = EvidenceKeyFor(command.Rule, command.EvidenceReference);
        ApplyEvidence(command.Evidence);
        Description = DomainGuard.RequireText(command.Description, DescriptionMaxLength, nameof(command.Description));
        RaisedAt = raisedAt.ToUniversalTime();
        Status = AlertStatus.Active;
    }

    /// <summary>Gets the evidence of an evaluated alert, or <see langword="null"/> for a manual alert.</summary>
    public AlertEvidence? Evidence =>
        EvidenceObservedValue is null || EvidenceUnit is null || EvidenceRecordedAt is null
            ? null
            : new AlertEvidence(EvidenceObservedValue.Value, EvidenceExpectedMinimum, EvidenceExpectedMaximum, EvidenceUnit,
                EvidenceRecordedAt.Value, EvidenceSourceRecordId, EvidenceSimulated);

    /// <summary>Gets whether the alert still requires attention (active or acknowledged).</summary>
    public bool IsOpen => Status is AlertStatus.Active or AlertStatus.Acknowledged;

    /// <summary>
    ///     Determines whether a participant of the group may see the alert: a delay concerns the whole group, while an
    ///     alert about a participant is visible only to that participant. Manual reports stay with the operation, since
    ///     their free text may describe other people.
    /// </summary>
    /// <param name="ownMemberIds">The manifest entries of the participant in the group of the alert.</param>
    /// <returns><see langword="true"/> when the participant may see the alert.</returns>
    public bool IsVisibleToParticipant(IReadOnlyCollection<int> ownMemberIds) =>
        Rule == SafetyRule.CheckpointDelay || (MemberId is { } memberId && ownMemberIds.Contains(memberId));

    /// <summary>Gets whether the alert is critical and has not been dismissed.</summary>
    public bool IsCritical => Severity == AlertSeverity.Critical && Status != AlertStatus.Dismissed;

    /// <summary>Acknowledges an active alert and records who acknowledged it and when.</summary>
    /// <param name="userId">The user who acknowledges the alert.</param>
    /// <param name="acknowledgedAt">The moment of the transition.</param>
    /// <exception cref="ArgumentException">Thrown when the user identifier is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the alert is not active.</exception>
    public void Acknowledge(int userId, DateTimeOffset acknowledgedAt)
    {
        DomainGuard.RequirePositive(userId, nameof(userId));
        if (Status != AlertStatus.Active)
            throw new InvalidOperationException("Only an active alert can be acknowledged.");
        Status = AlertStatus.Acknowledged;
        AcknowledgedByUserId = userId;
        AcknowledgedAt = acknowledgedAt.ToUniversalTime();
    }

    /// <summary>Dismisses an open alert as a false alarm and records who closed it and when.</summary>
    /// <param name="userId">The user who dismisses the alert.</param>
    /// <param name="dismissedAt">The moment of the transition.</param>
    /// <exception cref="ArgumentException">Thrown when the user identifier is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the alert is already closed.</exception>
    public void Dismiss(int userId, DateTimeOffset dismissedAt)
    {
        DomainGuard.RequirePositive(userId, nameof(userId));
        if (!IsOpen)
            throw new InvalidOperationException("Only an open alert can be dismissed.");
        Status = AlertStatus.Dismissed;
        Close(userId, dismissedAt);
    }

    /// <summary>Resolves an acknowledged alert and records who closed it and when.</summary>
    /// <param name="userId">The user who resolves the alert.</param>
    /// <param name="resolvedAt">The moment of the transition.</param>
    /// <exception cref="ArgumentException">Thrown when the user identifier is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the alert was not acknowledged.</exception>
    public void Resolve(int userId, DateTimeOffset resolvedAt)
    {
        DomainGuard.RequirePositive(userId, nameof(userId));
        if (Status != AlertStatus.Acknowledged)
            throw new InvalidOperationException("Only an acknowledged alert can be resolved.");
        Status = AlertStatus.Resolved;
        Close(userId, resolvedAt);
    }

    /// <summary>
    ///     Raises the severity of an open delay alert when the same delay worsens, refreshing its evidence and
    ///     description. A closed alert or a severity that is not higher leaves the alert unchanged, so reprocessing the
    ///     same evidence never reopens or downgrades it. The status is kept: an acknowledged alert stays acknowledged.
    /// </summary>
    /// <param name="severity">The severity of the worsened delay.</param>
    /// <param name="evidence">The refreshed elapsed time evidence.</param>
    /// <param name="description">The refreshed description.</param>
    /// <returns><see langword="true"/> when the severity was raised.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the alert was not produced by the delay rule.</exception>
    /// <exception cref="ArgumentException">Thrown when the severity, evidence or description is invalid.</exception>
    public bool Escalate(AlertSeverity severity, AlertEvidence evidence, string description)
    {
        if (Rule != SafetyRule.CheckpointDelay)
            throw new InvalidOperationException("Only a delay alert can be escalated.");
        if (!Enum.IsDefined(severity))
            throw new ArgumentException("Unknown alert severity.", nameof(severity));
        ArgumentNullException.ThrowIfNull(evidence);
        RequireRuleScope(Rule, CheckpointId, MemberId, evidence);
        var escalatedDescription = DomainGuard.RequireText(description, DescriptionMaxLength, nameof(description));
        if (!IsOpen || severity <= Severity) return false;
        Severity = severity;
        ApplyEvidence(evidence);
        Description = escalatedDescription;
        return true;
    }

    /// <summary>
    ///     Determines the severity of a delay: high once the maximum expected time is exceeded and critical once
    ///     the elapsed time doubles it.
    /// </summary>
    /// <param name="elapsed">The time elapsed since the group passed the origin checkpoint.</param>
    /// <param name="maximumMinutes">The maximum expected minutes of the segment.</param>
    /// <returns>The delay severity, or <see langword="null"/> when the segment is still within its window.</returns>
    public static AlertSeverity? DelaySeverityFor(TimeSpan elapsed, int maximumMinutes)
    {
        if (maximumMinutes <= 0)
            throw new ArgumentException("Maximum minutes must be greater than zero.", nameof(maximumMinutes));
        var maximum = TimeSpan.FromMinutes(maximumMinutes);
        if (elapsed <= maximum) return null;
        return elapsed >= maximum * 2 ? AlertSeverity.Critical : AlertSeverity.High;
    }

    /// <summary>
    ///     Builds the identity of an evidence within a session: the rule code and the reference of the evidence.
    /// </summary>
    /// <param name="rule">The rule that evaluates the evidence.</param>
    /// <param name="reference">The reference of the evidence within its rule.</param>
    /// <returns>The evidence identity.</returns>
    /// <exception cref="ArgumentException">Thrown when the rule is unknown or the reference is empty or too long.</exception>
    public static string EvidenceKeyFor(SafetyRule rule, string reference)
    {
        if (!Enum.IsDefined(rule))
            throw new ArgumentException("Unknown safety rule.", nameof(rule));
        return $"{rule.ToCode()}:{DomainGuard.RequireText(reference, EvidenceReferenceMaxLength, nameof(reference))}";
    }

    private static void RequireRuleScope(SafetyRule rule, int? checkpointId, int? memberId, AlertEvidence evidence)
    {
        switch (rule)
        {
            case SafetyRule.HeartRateOutsideBaseline:
                if (memberId is null || evidence.Unit != AlertEvidence.BeatsPerMinute)
                    throw new ArgumentException("A heart rate alert requires a participant and a heart rate evidence.",
                        nameof(rule));
                break;
            case SafetyRule.SimulatedRouteDeviation:
                if (memberId is null || evidence.Unit != AlertEvidence.Meters || !evidence.Simulated)
                    throw new ArgumentException(
                        "A route deviation alert requires a participant and a simulated distance evidence.", nameof(rule));
                break;
            case SafetyRule.CheckpointDelay:
                if (checkpointId is null || memberId is not null || evidence.Unit != AlertEvidence.Minutes)
                    throw new ArgumentException(
                        "A delay alert concerns the group at a checkpoint and requires an elapsed time evidence.",
                        nameof(rule));
                break;
        }
    }

    private void Close(int userId, DateTimeOffset closedAt)
    {
        ClosedByUserId = userId;
        ClosedAt = closedAt.ToUniversalTime();
    }

    private void ApplyEvidence(AlertEvidence evidence)
    {
        EvidenceObservedValue = evidence.ObservedValue;
        EvidenceExpectedMinimum = evidence.ExpectedMinimum;
        EvidenceExpectedMaximum = evidence.ExpectedMaximum;
        EvidenceUnit = evidence.Unit;
        EvidenceRecordedAt = evidence.RecordedAt;
        EvidenceSourceRecordId = evidence.SourceRecordId;
        EvidenceSimulated = evidence.Simulated;
    }
}
