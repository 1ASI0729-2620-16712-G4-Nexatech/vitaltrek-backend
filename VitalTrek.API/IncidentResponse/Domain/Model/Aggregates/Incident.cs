using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.Entities;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing an incident reported during an active expedition, such as a medical emergency, an
///     accident, a missing participant or an environmental hazard. Every transition is explicit and is recorded with its
///     actor, moment and note.
/// </summary>
public partial class Incident
{
    /// <summary>Maximum length allowed for the incident description.</summary>
    public const int MaximumDescriptionLength = 500;

    /// <summary>Tolerance accepted between the clocks of the field devices and the server.</summary>
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    private readonly List<IncidentStatusChange> _statusChanges = [];

    /// <summary>Gets the incident identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the tracking session during which the incident occurred.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the expedition group tracked by the session, resolved by the server when the incident is reported.</summary>
    public int ExpeditionGroupId { get; private set; }

    /// <summary>Gets the agency that owns the incident.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the early warning alert that originated the incident, or null for a manual report.</summary>
    public int? AlertId { get; private set; }

    /// <summary>Gets the user that reported the incident, or null for rows of the first version.</summary>
    public int? ReportedByUserId { get; private set; }

    /// <summary>Gets the moment the incident was reported in the field.</summary>
    public DateTimeOffset ReportedAt { get; private set; }

    /// <summary>Gets the nature of the incident.</summary>
    public IncidentType IncidentType { get; private set; }

    /// <summary>Gets the severity assigned to the incident.</summary>
    public IncidentSeverity Severity { get; private set; }

    /// <summary>Gets the description of what happened.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the lifecycle state of the incident.</summary>
    public IncidentStatus Status { get; private set; }

    /// <summary>Gets the moment the assigned field guide confirmed the incident.</summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>Gets the moment the incident was dismissed.</summary>
    public DateTimeOffset? DismissedAt { get; private set; }

    /// <summary>Gets the moment an operations administrator declared the emergency.</summary>
    public DateTimeOffset? EmergencyDeclaredAt { get; private set; }

    /// <summary>Gets the moment the incident was resolved, or null while it remains open.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Gets the moment the incident was closed.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>Gets the recorded transitions, oldest first.</summary>
    public IReadOnlyList<IncidentStatusChange> StatusChanges => _statusChanges;

    /// <summary>Gets a value indicating whether the incident still requires attention.</summary>
    public bool IsOpen => Status.IsOpen();

    /// <summary>
    ///     Initializes a new instance of the <see cref="Incident"/> class for Entity Framework Core.
    /// </summary>
    public Incident()
    {
    }

    /// <summary>
    ///     Reports a new incident. The incident always starts in the Reported state.
    /// </summary>
    /// <param name="command">The declare incident command.</param>
    /// <param name="expeditionGroupId">The expedition group tracked by the session, resolved by the server.</param>
    /// <param name="reportedByUserId">The authenticated user that reports the incident.</param>
    /// <param name="declaredAt">The moment the report reaches the platform.</param>
    /// <exception cref="ArgumentException">Thrown when the incident data is invalid.</exception>
    public Incident(DeclareIncidentCommand command, int expeditionGroupId, int reportedByUserId,
        DateTimeOffset declaredAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        ExpeditionGroupId = DomainGuard.RequirePositive(expeditionGroupId, nameof(expeditionGroupId));
        ReportedByUserId = DomainGuard.RequirePositive(reportedByUserId, nameof(reportedByUserId));
        Description = DomainGuard.RequireText(command.Description, MaximumDescriptionLength, nameof(command.Description));
        if (command.AlertId is not null) AlertId = DomainGuard.RequirePositive(command.AlertId.Value, nameof(command.AlertId));
        if (!Enum.IsDefined(command.IncidentType))
            throw new ArgumentException("IncidentType is not a known incident type.", nameof(command));
        if (!Enum.IsDefined(command.Severity))
            throw new ArgumentException("Severity is not a known incident severity.", nameof(command));
        if (command.ReportedAt > declaredAt + ClockTolerance)
            throw new ArgumentException("ReportedAt cannot be in the future.", nameof(command));
        IncidentType = command.IncidentType;
        Severity = command.Severity;
        ReportedAt = command.ReportedAt;
        Status = IncidentStatus.Reported;
        _statusChanges.Add(new IncidentStatusChange(null, IncidentStatus.Reported, reportedByUserId, null, ReportedAt));
    }

    /// <summary>Returns true when the incident can move from its current state to <paramref name="target"/>.</summary>
    public bool CanMoveTo(IncidentStatus target) => (Status, target) switch
    {
        (IncidentStatus.Reported, IncidentStatus.Confirmed) => true,
        (IncidentStatus.Reported, IncidentStatus.Dismissed) => true,
        (IncidentStatus.Confirmed, IncidentStatus.EmergencyDeclared) => true,
        (IncidentStatus.Confirmed, IncidentStatus.Resolved) => true,
        (IncidentStatus.EmergencyDeclared, IncidentStatus.Resolved) => true,
        (IncidentStatus.Resolved, IncidentStatus.Closed) => true,
        _ => false
    };

    /// <summary>The assigned field guide verifies the reported incident in the field.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident is not reported.</exception>
    public void Confirm(int actorUserId, DateTimeOffset confirmedAt, string? note = null)
    {
        MoveTo(IncidentStatus.Confirmed, actorUserId, confirmedAt, note);
        ConfirmedAt = confirmedAt;
    }

    /// <summary>The assigned field guide discards a reported incident as a false report.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident is not reported.</exception>
    public void Dismiss(int actorUserId, DateTimeOffset dismissedAt, string? note = null)
    {
        MoveTo(IncidentStatus.Dismissed, actorUserId, dismissedAt, note);
        DismissedAt = dismissedAt;
    }

    /// <summary>An operations administrator declares an emergency for a confirmed incident.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident is not confirmed.</exception>
    public void DeclareEmergency(int actorUserId, DateTimeOffset declaredAt, string? note = null)
    {
        MoveTo(IncidentStatus.EmergencyDeclared, actorUserId, declaredAt, note);
        EmergencyDeclaredAt = declaredAt;
    }

    /// <summary>Resolves a confirmed incident or a declared emergency and records the moment of its resolution.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident cannot be resolved from its state.</exception>
    /// <exception cref="ArgumentException">Thrown when the resolution precedes the report.</exception>
    public void Resolve(int actorUserId, DateTimeOffset resolvedAt, string? note = null)
    {
        if (resolvedAt + ClockTolerance < ReportedAt)
            throw new ArgumentException("ResolvedAt cannot be earlier than ReportedAt.", nameof(resolvedAt));
        MoveTo(IncidentStatus.Resolved, actorUserId, resolvedAt, note);
        ResolvedAt = resolvedAt;
    }

    /// <summary>Closes a resolved incident. A closed incident admits no further changes.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident has not been resolved.</exception>
    public void Close(int actorUserId, DateTimeOffset closedAt, string? note = null)
    {
        MoveTo(IncidentStatus.Closed, actorUserId, closedAt, note);
        ClosedAt = closedAt;
    }

    /// <summary>
    ///     Returns true when an evacuation action captured at <paramref name="recordedAt"/> belongs to the emergency of this
    ///     incident: the capture happened after the declaration and, when the incident was already resolved, before the
    ///     resolution. A late transmission of a capture made during the emergency is therefore still accepted.
    /// </summary>
    public bool AcceptsEvacuationCapturedAt(DateTimeOffset recordedAt)
    {
        if (EmergencyDeclaredAt is null || recordedAt + ClockTolerance < EmergencyDeclaredAt) return false;
        return Status switch
        {
            IncidentStatus.EmergencyDeclared => true,
            IncidentStatus.Resolved or IncidentStatus.Closed => ResolvedAt is not null &&
                                                                 recordedAt <= ResolvedAt + ClockTolerance,
            _ => false
        };
    }

    private void MoveTo(IncidentStatus target, int actorUserId, DateTimeOffset occurredAt, string? note)
    {
        if (!CanMoveTo(target))
            throw new InvalidOperationException(
                $"An incident cannot move from {Status.ToCode()} to {target.ToCode()}.");
        var change = new IncidentStatusChange(Status, target, DomainGuard.RequirePositive(actorUserId, nameof(actorUserId)),
            note, occurredAt);
        Status = target;
        _statusChanges.Add(change);
    }
}
