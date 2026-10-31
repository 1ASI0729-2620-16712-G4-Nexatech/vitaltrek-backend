namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while preparing a participant.
/// </summary>
public enum EParticipantPreparationError
{
    /// <summary>The preparation data is invalid.</summary>
    InvalidData,

    /// <summary>The preparation does not exist or does not belong to the requester.</summary>
    ParticipantPreparationNotFound,

    /// <summary>The group state does not allow this change.</summary>
    ExpeditionGroupNotModifiable,

    /// <summary>The wearable device does not exist in the agency.</summary>
    WearableDeviceNotFound,

    /// <summary>The wearable device is assigned to another participant or is not available.</summary>
    WearableDeviceUnavailable,

    /// <summary>A wearable device must be assigned before granting consent.</summary>
    WearableDeviceRequired,

    /// <summary>An emergency contact must be registered before granting consent.</summary>
    EmergencyContactRequired,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
