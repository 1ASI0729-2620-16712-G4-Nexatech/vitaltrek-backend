namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while registering a wearable device.
/// </summary>
public enum ECreateWearableDeviceError
{
    /// <summary>The device data is invalid.</summary>
    InvalidData,

    /// <summary>The inventory code is already registered.</summary>
    DuplicateCode,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
