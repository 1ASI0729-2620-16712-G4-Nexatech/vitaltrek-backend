using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the official path followed by an adventure tour.
/// </summary>
public partial class Route
{
    /// <summary>Gets the route identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that operates the route.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the route name, unique within the agency.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the starting location.</summary>
    public string Origin { get; private set; }

    /// <summary>Gets the ending location.</summary>
    public string Destination { get; private set; }

    /// <summary>Gets the total distance in kilometers.</summary>
    public decimal DistanceKm { get; private set; }

    /// <summary>Gets the accumulated elevation gain in meters.</summary>
    public int ElevationGainMeters { get; private set; }

    /// <summary>Gets the difficulty level.</summary>
    public RouteDifficulty Difficulty { get; private set; }

    /// <summary>Gets the estimated duration in minutes.</summary>
    public int EstimatedDurationMinutes { get; private set; }

    /// <summary>Gets the configuration status.</summary>
    public RouteStatus Status { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Route"/> class for Entity Framework Core.
    /// </summary>
    public Route()
    {
        Name = string.Empty;
        Origin = string.Empty;
        Destination = string.Empty;
    }

    /// <summary>
    ///     Initializes a new route in <see cref="RouteStatus.Draft"/> status.
    /// </summary>
    /// <param name="command">The create route command.</param>
    /// <exception cref="ArgumentException">Thrown when the route data is invalid.</exception>
    public Route(CreateRouteCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        Name = DomainGuard.RequireText(command.Name, 120, nameof(command.Name));
        Origin = DomainGuard.RequireText(command.Origin, 120, nameof(command.Origin));
        Destination = DomainGuard.RequireText(command.Destination, 120, nameof(command.Destination));
        if (command.DistanceKm <= 0)
            throw new ArgumentException("Distance must be greater than zero.", nameof(command));
        if (command.ElevationGainMeters < 0)
            throw new ArgumentException("Elevation gain cannot be negative.", nameof(command));
        DistanceKm = command.DistanceKm;
        ElevationGainMeters = command.ElevationGainMeters;
        Difficulty = command.Difficulty;
        EstimatedDurationMinutes = DomainGuard.RequirePositive(command.EstimatedDurationMinutes,
            nameof(command.EstimatedDurationMinutes));
        Status = RouteStatus.Draft;
    }

    /// <summary>
    ///     Gets a value indicating whether the route can be used to plan tours.
    /// </summary>
    public bool IsEnabled => Status == RouteStatus.Enabled;

    /// <summary>
    ///     Enables the route for tour planning.
    /// </summary>
    /// <param name="checkpointCount">The number of checkpoints registered for the route.</param>
    /// <exception cref="InvalidOperationException">Thrown when the route has no checkpoints.</exception>
    public void Enable(int checkpointCount)
    {
        if (checkpointCount < 1)
            throw new InvalidOperationException("A route requires at least one checkpoint to be enabled.");
        Status = RouteStatus.Enabled;
    }
}
