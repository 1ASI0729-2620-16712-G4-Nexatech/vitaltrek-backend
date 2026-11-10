namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle state of an incident. The progression is Reported, InResponse, Resolved and Closed.
/// </summary>
public enum IncidentStatus
{
    /// <summary>The incident was declared and no response has started yet.</summary>
    Reported,

    /// <summary>A response to the incident is in progress.</summary>
    InResponse,

    /// <summary>The incident was resolved in the field.</summary>
    Resolved,

    /// <summary>The incident was closed after its resolution and admits no further changes.</summary>
    Closed
}

/// <summary>
///     Provides conversions between <see cref="IncidentStatus"/> values and their external codes.
/// </summary>
public static class IncidentStatusExtensions
{
    /// <summary>Converts the status into its external lowercase code.</summary>
    public static string ToCode(this IncidentStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into an <see cref="IncidentStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static IncidentStatus FromCode(string code) =>
        Enum.TryParse<IncidentStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown incident status '{code}'.", nameof(code));
}
