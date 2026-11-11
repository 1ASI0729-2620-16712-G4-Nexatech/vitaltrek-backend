using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing an incident declared during an active expedition, such as a medical
///     emergency, an accident, a missing participant or an environmental hazard.
/// </summary>
public partial class Incident
{
    /// <summary>Maximum length allowed for the incident description.</summary>
    public const int MaximumDescriptionLength = 500;

    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    /// <summary>Gets the incident identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the tracking session during which the incident occurred.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the agency that owns the incident.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the moment the incident was reported.</summary>
    public DateTimeOffset ReportedAt { get; private set; }

    /// <summary>Gets the nature of the incident.</summary>
    public IncidentType IncidentType { get; private set; }

    /// <summary>Gets the severity assigned to the incident.</summary>
    public IncidentSeverity Severity { get; private set; }

    /// <summary>Gets the description of what happened.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the lifecycle state of the incident.</summary>
    public IncidentStatus Status { get; private set; }

    /// <summary>Gets the moment the incident was resolved, or null while it remains open.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Gets a value indicating whether the incident is still open.</summary>
    public bool IsOpen => Status is IncidentStatus.Reported or IncidentStatus.InResponse;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Incident"/> class for Entity Framework Core.
    /// </summary>
    public Incident()
    {
    }

    /// <summary>
    ///     Declares a new incident. The incident always starts in the Reported state.
    /// </summary>
    /// <param name="command">The declare incident command.</param>
    /// <param name="declaredAt">The moment the declaration reaches the platform.</param>
    /// <exception cref="ArgumentException">Thrown when the incident data is invalid.</exception>
    public Incident(DeclareIncidentCommand command, DateTimeOffset declaredAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        Description = DomainGuard.RequireText(command.Description, MaximumDescriptionLength, nameof(command.Description));
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
    }

    /// <summary>
    ///     Marks that a response to the incident has started.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident is not in the Reported state.</exception>
    public void MarkInResponse()
    {
        if (Status != IncidentStatus.Reported)
            throw new InvalidOperationException("Only a reported incident can move to the in response state.");
        Status = IncidentStatus.InResponse;
    }

    /// <summary>
    ///     Resolves the incident and records the moment of its resolution.
    /// </summary>
    /// <param name="resolvedAt">The moment the incident was resolved.</param>
    /// <exception cref="InvalidOperationException">Thrown when the incident is no longer open.</exception>
    /// <exception cref="ArgumentException">Thrown when the resolution precedes the report.</exception>
    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Only an open incident can be resolved.");
        if (resolvedAt + ClockTolerance < ReportedAt)
            throw new ArgumentException("ResolvedAt cannot be earlier than ReportedAt.", nameof(resolvedAt));
        Status = IncidentStatus.Resolved;
        ResolvedAt = resolvedAt;
    }

    /// <summary>
    ///     Closes a resolved incident. A closed incident admits no further changes.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the incident has not been resolved.</exception>
    public void Close()
    {
        if (Status != IncidentStatus.Resolved)
            throw new InvalidOperationException("Only a resolved incident can be closed.");
        Status = IncidentStatus.Closed;
    }
}
