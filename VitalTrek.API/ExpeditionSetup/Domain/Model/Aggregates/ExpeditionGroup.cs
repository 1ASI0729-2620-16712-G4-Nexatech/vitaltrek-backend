using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a group of tourists that travels a route on a given date under the
///     responsibility of a field guide. It is the minimum unit of operational supervision.
/// </summary>
public partial class ExpeditionGroup
{
    /// <summary>Maximum number of participants allowed for any group.</summary>
    public const int MaximumAllowedCapacity = 100;

    /// <summary>Gets the group identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that operates the group.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the route the group travels.</summary>
    public int RouteId { get; private set; }

    /// <summary>Gets the group name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the scheduled departure date.</summary>
    public DateOnly DepartureDate { get; private set; }

    /// <summary>Gets the maximum number of participants.</summary>
    public int MaximumCapacity { get; private set; }

    /// <summary>Gets the assigned field guide, if any.</summary>
    public int? FieldGuideId { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public ExpeditionGroupStatus Status { get; private set; }

    /// <summary>Gets the moment the tour started.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets the moment the tour was closed.</summary>
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExpeditionGroup"/> class for Entity Framework Core.
    /// </summary>
    public ExpeditionGroup()
    {
        Name = string.Empty;
    }

    /// <summary>
    ///     Initializes a new scheduled expedition group.
    /// </summary>
    /// <param name="command">The create expedition group command.</param>
    /// <param name="today">The current local date of the agency.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or the departure date is in the past.</exception>
    public ExpeditionGroup(CreateExpeditionGroupCommand command, DateOnly today)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        RouteId = DomainGuard.RequirePositive(command.RouteId, nameof(command.RouteId));
        Name = DomainGuard.RequireText(command.Name, 120, nameof(command.Name));
        if (command.DepartureDate < today)
            throw new ArgumentException("Departure date cannot be in the past.", nameof(command));
        if (command.MaximumCapacity is < 1 or > MaximumAllowedCapacity)
            throw new ArgumentException($"Maximum capacity must be between 1 and {MaximumAllowedCapacity}.", nameof(command));
        DepartureDate = command.DepartureDate;
        MaximumCapacity = command.MaximumCapacity;
        Status = ExpeditionGroupStatus.Scheduled;
    }

    /// <summary>Gets a value indicating whether the group can still be configured.</summary>
    public bool IsScheduled => Status == ExpeditionGroupStatus.Scheduled;

    /// <summary>
    ///     Assigns the field guide responsible for the group.
    /// </summary>
    /// <param name="fieldGuideId">The field guide identifier.</param>
    /// <exception cref="InvalidOperationException">Thrown when the group is no longer scheduled.</exception>
    public void AssignFieldGuide(int fieldGuideId)
    {
        EnsureScheduled();
        FieldGuideId = DomainGuard.RequirePositive(fieldGuideId, nameof(fieldGuideId));
    }

    /// <summary>
    ///     Starts the adventure tour. The start time is recorded only once.
    /// </summary>
    /// <param name="startedAt">The moment the tour starts.</param>
    /// <exception cref="InvalidOperationException">Thrown when the group has no field guide or is not scheduled.</exception>
    public void Start(DateTimeOffset startedAt)
    {
        if (Status == ExpeditionGroupStatus.Active) return;
        EnsureScheduled();
        if (FieldGuideId is null)
            throw new InvalidOperationException("An adventure tour requires a responsible field guide.");
        Status = ExpeditionGroupStatus.Active;
        StartedAt = startedAt;
    }

    /// <summary>
    ///     Formally closes the adventure tour.
    /// </summary>
    /// <param name="finishedAt">The moment the tour is closed.</param>
    /// <exception cref="InvalidOperationException">Thrown when the tour is not active.</exception>
    public void Finish(DateTimeOffset finishedAt)
    {
        if (Status != ExpeditionGroupStatus.Active)
            throw new InvalidOperationException("Only an active adventure tour can be closed.");
        Status = ExpeditionGroupStatus.Finished;
        FinishedAt = finishedAt;
    }

    private void EnsureScheduled()
    {
        if (!IsScheduled) throw new InvalidOperationException("The expedition group is no longer scheduled.");
    }
}
