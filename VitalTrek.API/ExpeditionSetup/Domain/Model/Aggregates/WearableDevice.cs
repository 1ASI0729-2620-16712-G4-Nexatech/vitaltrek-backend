using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a wearable device of the agency inventory that records position and vital signs.
/// </summary>
public partial class WearableDevice
{
    /// <summary>Gets the device identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the device.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the inventory code of the device.</summary>
    public string Code { get; private set; }

    /// <summary>Gets the inventory status.</summary>
    public WearableDeviceStatus Status { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="WearableDevice"/> class for Entity Framework Core.
    /// </summary>
    public WearableDevice()
    {
        Code = string.Empty;
    }

    /// <summary>
    ///     Initializes a new available wearable device.
    /// </summary>
    /// <param name="command">The create wearable device command.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid.</exception>
    public WearableDevice(CreateWearableDeviceCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        Code = DomainGuard.RequireText(command.Code, 40, nameof(command.Code)).ToUpperInvariant();
        Status = WearableDeviceStatus.Available;
    }

    /// <summary>Gets a value indicating whether the device can be assigned.</summary>
    public bool IsAvailable => Status == WearableDeviceStatus.Available;

    /// <summary>
    ///     Marks the device as assigned to a participant.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the device is not available.</exception>
    public void MarkAssigned()
    {
        if (!IsAvailable) throw new InvalidOperationException("The wearable device is not available.");
        Status = WearableDeviceStatus.Assigned;
    }

    /// <summary>
    ///     Returns the device to the inventory with the condition reported at closure.
    /// </summary>
    /// <param name="condition">Available for devices in good condition, maintenance or lost otherwise.</param>
    /// <exception cref="ArgumentException">Thrown when the condition is <see cref="WearableDeviceStatus.Assigned"/>.</exception>
    public void Release(WearableDeviceStatus condition = WearableDeviceStatus.Available)
    {
        if (condition == WearableDeviceStatus.Assigned)
            throw new ArgumentException("A released device cannot remain assigned.", nameof(condition));
        Status = condition;
    }
}
