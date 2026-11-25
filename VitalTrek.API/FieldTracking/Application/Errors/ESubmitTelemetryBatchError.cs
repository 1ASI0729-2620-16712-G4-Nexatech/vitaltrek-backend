namespace VitalTrek.API.FieldTracking.Application.Errors;

/// <summary>
///     Error states that may occur while submitting a telemetry batch.
/// </summary>
public enum ESubmitTelemetryBatchError
{
    /// <summary>The actor is not an authenticated field guide in the command's agency.</summary>
    AccessForbidden,

    /// <summary>One or more records contain invalid coordinate values.</summary>
    InvalidData,

    /// <summary>The referenced session was not found or does not belong to the agency.</summary>
    SessionNotFound,

    /// <summary>The session is not active and cannot accept new telemetry.</summary>
    SessionNotActive,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
