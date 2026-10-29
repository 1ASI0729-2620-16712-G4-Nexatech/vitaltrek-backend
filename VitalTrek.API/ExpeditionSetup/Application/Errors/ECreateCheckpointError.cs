namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while registering a checkpoint.
/// </summary>
public enum ECreateCheckpointError
{
    /// <summary>The checkpoint data violates the checkpoint invariants.</summary>
    InvalidData,

    /// <summary>The route does not exist in the agency.</summary>
    RouteNotFound,

    /// <summary>The route already has a checkpoint in the same position.</summary>
    DuplicateSequenceOrder,

    /// <summary>The Bluetooth identifier is already registered.</summary>
    DuplicateBluetoothId,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
