using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the time range in which a group is expected to cover the segment
///     between two consecutive checkpoints. It is the reference used to detect delays.
/// </summary>
public partial class ExpectedTimeWindow
{
    /// <summary>Gets the window identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the window.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the route of the segment.</summary>
    public int RouteId { get; private set; }

    /// <summary>Gets the checkpoint where the segment starts.</summary>
    public int FromCheckpointId { get; private set; }

    /// <summary>Gets the checkpoint where the segment ends.</summary>
    public int ToCheckpointId { get; private set; }

    /// <summary>Gets the minimum expected duration of the segment in minutes.</summary>
    public int MinimumMinutes { get; private set; }

    /// <summary>Gets the maximum expected duration of the segment in minutes.</summary>
    public int MaximumMinutes { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExpectedTimeWindow"/> class for Entity Framework Core.
    /// </summary>
    public ExpectedTimeWindow()
    {
    }

    /// <summary>
    ///     Initializes a new expected time window.
    /// </summary>
    /// <param name="command">The create expected time window command.</param>
    /// <exception cref="ArgumentException">Thrown when the limits are inconsistent.</exception>
    public ExpectedTimeWindow(CreateExpectedTimeWindowCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        RouteId = DomainGuard.RequirePositive(command.RouteId, nameof(command.RouteId));
        FromCheckpointId = DomainGuard.RequirePositive(command.FromCheckpointId, nameof(command.FromCheckpointId));
        ToCheckpointId = DomainGuard.RequirePositive(command.ToCheckpointId, nameof(command.ToCheckpointId));
        if (FromCheckpointId == ToCheckpointId)
            throw new ArgumentException("A segment requires two different checkpoints.", nameof(command));
        if (command.MinimumMinutes < 0)
            throw new ArgumentException("Minimum minutes cannot be negative.", nameof(command));
        if (command.MaximumMinutes <= command.MinimumMinutes)
            throw new ArgumentException("Maximum minutes must be greater than minimum minutes.", nameof(command));
        MinimumMinutes = command.MinimumMinutes;
        MaximumMinutes = command.MaximumMinutes;
    }
}
