namespace VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle status of a tracking session.
/// </summary>
public enum SessionStatus
{
    /// <summary>Telemetry is being received.</summary>
    Active,

    /// <summary>Session was closed normally.</summary>
    Completed,

    /// <summary>Session was paused due to connectivity or operational reasons.</summary>
    Suspended
}

/// <summary>
///     Extension methods for <see cref="SessionStatus"/>.
/// </summary>
public static class SessionStatusExtensions
{
    /// <summary>Converts the status to its lowercase string code stored in the database.</summary>
    public static string ToCode(this SessionStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Parses a database code back to <see cref="SessionStatus"/>.</summary>
    /// <exception cref="ArgumentException">When the code is unrecognised.</exception>
    public static SessionStatus FromCode(string code) =>
        Enum.TryParse<SessionStatus>(code, true, out var status)
            ? status
            : throw new ArgumentException($"Unknown session status code: '{code}'.", nameof(code));
}
