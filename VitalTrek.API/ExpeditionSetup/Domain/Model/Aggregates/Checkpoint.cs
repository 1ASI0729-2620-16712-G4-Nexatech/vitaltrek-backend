using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a predefined physical location of a route where the group's progress is confirmed
///     and where wearables transmit their accumulated telemetry.
/// </summary>
public partial class Checkpoint
{
    /// <summary>Gets the checkpoint identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the checkpoint.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the route the checkpoint belongs to.</summary>
    public int RouteId { get; private set; }

    /// <summary>Gets the location name.</summary>
    public string Location { get; private set; }

    /// <summary>Gets the position of the checkpoint within the route, starting at 1.</summary>
    public int SequenceOrder { get; private set; }

    /// <summary>Gets the Bluetooth identifier of the physical checkpoint unit.</summary>
    public string BluetoothId { get; private set; }

    /// <summary>Gets the latitude in decimal degrees.</summary>
    public double Latitude { get; private set; }

    /// <summary>Gets the longitude in decimal degrees.</summary>
    public double Longitude { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Checkpoint"/> class for Entity Framework Core.
    /// </summary>
    public Checkpoint()
    {
        Location = string.Empty;
        BluetoothId = string.Empty;
    }

    /// <summary>
    ///     Initializes a new checkpoint.
    /// </summary>
    /// <param name="command">The create checkpoint command.</param>
    /// <exception cref="ArgumentException">Thrown when the checkpoint data is invalid.</exception>
    public Checkpoint(CreateCheckpointCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        RouteId = DomainGuard.RequirePositive(command.RouteId, nameof(command.RouteId));
        Location = DomainGuard.RequireText(command.Location, 120, nameof(command.Location));
        SequenceOrder = DomainGuard.RequirePositive(command.SequenceOrder, nameof(command.SequenceOrder));
        BluetoothId = DomainGuard.RequireText(command.BluetoothId, 50, nameof(command.BluetoothId)).ToUpperInvariant();
        if (!double.IsFinite(command.Latitude) || Math.Abs(command.Latitude) > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.", nameof(command));
        if (!double.IsFinite(command.Longitude) || Math.Abs(command.Longitude) > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.", nameof(command));
        Latitude = command.Latitude;
        Longitude = command.Longitude;
    }
}
