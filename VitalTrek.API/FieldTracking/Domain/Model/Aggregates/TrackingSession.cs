using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.FieldTracking.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a GPS tracking session for an expedition group.
/// </summary>
public partial class TrackingSession
{
    /// <summary>Gets the session identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns this session.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the expedition group being tracked.</summary>
    public int ExpeditionGroupId { get; private set; }

    /// <summary>Gets when the session was started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Gets when the session was closed, or null if still active.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Gets the current lifecycle status.</summary>
    public SessionStatus Status { get; private set; }

    /// <summary>
    ///     Initializes a new instance of <see cref="TrackingSession"/> for Entity Framework Core.
    /// </summary>
    public TrackingSession()
    {
    }

    /// <summary>
    ///     Starts a new tracking session for an expedition group.
    /// </summary>
    /// <param name="command">The start tracking session command.</param>
    /// <exception cref="ArgumentException">Thrown when the command data is invalid.</exception>
    public TrackingSession(StartTrackingSessionCommand command) : this(command, DateTimeOffset.UtcNow)
    {
    }

    /// <summary>Starts a session using the same timestamp as the expedition group.</summary>
    public TrackingSession(StartTrackingSessionCommand command, DateTimeOffset startedAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        ExpeditionGroupId = DomainGuard.RequirePositive(command.ExpeditionGroupId, nameof(command.ExpeditionGroupId));
        // Session columns predate telemetry precision and store whole seconds.
        StartedAt = DateTimeOffset.FromUnixTimeSeconds(startedAt.ToUnixTimeSeconds());
        Status = SessionStatus.Active;
    }

    /// <summary>Gets a value indicating whether this session is currently receiving telemetry.</summary>
    public bool IsActive => Status == SessionStatus.Active;

    /// <summary>Closes this session, recording the end time.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active.</exception>
    public void End() => End(DateTimeOffset.UtcNow);

    /// <summary>Stages completion at the timestamp supplied by Tour Closure.</summary>
    public void End(DateTimeOffset endedAt)
    {
        if (Status != SessionStatus.Active)
            throw new InvalidOperationException("Only an active session can be ended.");
        if (endedAt < StartedAt)
            throw new ArgumentException("The session cannot end before it starts.", nameof(endedAt));
        Status = SessionStatus.Completed;
        EndedAt = DateTimeOffset.FromUnixTimeSeconds(endedAt.ToUnixTimeSeconds());
    }
}
