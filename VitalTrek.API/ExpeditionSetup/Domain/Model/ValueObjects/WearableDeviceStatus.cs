namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Inventory status of a wearable device.
/// </summary>
public enum WearableDeviceStatus
{
    /// <summary>The device can be assigned to a participant.</summary>
    Available,

    /// <summary>The device is assigned to a participant of an expedition.</summary>
    Assigned,

    /// <summary>The device was returned damaged and needs maintenance.</summary>
    Maintenance,

    /// <summary>The device was not returned.</summary>
    Lost
}

/// <summary>
///     Provides conversions between <see cref="WearableDeviceStatus"/> values and their external codes.
/// </summary>
public static class WearableDeviceStatusExtensions
{
    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this WearableDeviceStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into a <see cref="WearableDeviceStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static WearableDeviceStatus FromCode(string code) =>
        Enum.TryParse<WearableDeviceStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown wearable device status '{code}'.", nameof(code));
}
