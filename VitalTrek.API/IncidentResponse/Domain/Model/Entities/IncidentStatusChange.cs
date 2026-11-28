using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Entities;

/// <summary>
///     Entity of the <see cref="Aggregates.Incident"/> aggregate that records one transition: who executed it, when and why.
/// </summary>
public class IncidentStatusChange
{
    /// <summary>Maximum length allowed for the transition note.</summary>
    public const int MaximumNoteLength = 500;

    /// <summary>Gets the identifier of the change.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the incident that changed.</summary>
    public int IncidentId { get; private set; }

    /// <summary>Gets the previous state, or null for the initial report.</summary>
    public IncidentStatus? FromStatus { get; private set; }

    /// <summary>Gets the state reached by the transition.</summary>
    public IncidentStatus ToStatus { get; private set; }

    /// <summary>Gets the user that executed the transition.</summary>
    public int ActorUserId { get; private set; }

    /// <summary>Gets the optional note that justifies the transition.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets the moment of the transition.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Initializes a new instance of the <see cref="IncidentStatusChange"/> class for Entity Framework Core.</summary>
    public IncidentStatusChange()
    {
    }

    /// <summary>Records a transition.</summary>
    /// <exception cref="ArgumentException">Thrown when the note is too long.</exception>
    public IncidentStatusChange(IncidentStatus? fromStatus, IncidentStatus toStatus, int actorUserId, string? note,
        DateTimeOffset occurredAt)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is { Length: > MaximumNoteLength })
            throw new ArgumentException($"Note cannot exceed {MaximumNoteLength} characters.", nameof(note));
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ActorUserId = actorUserId;
        Note = trimmed;
        OccurredAt = occurredAt;
    }
}
