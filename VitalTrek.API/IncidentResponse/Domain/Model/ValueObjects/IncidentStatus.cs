namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle state of an incident. A reported incident is confirmed or dismissed by the assigned field guide; only an
///     operations administrator declares an emergency for a confirmed incident; a confirmed or emergency incident is
///     resolved and a resolved incident is closed. Dismissed and closed incidents admit no further transitions.
/// </summary>
public enum IncidentStatus
{
    /// <summary>The incident was reported and is waiting for field verification.</summary>
    Reported,

    /// <summary>The assigned field guide verified the incident in the field.</summary>
    Confirmed,

    /// <summary>The incident was verified as a false report. Terminal state.</summary>
    Dismissed,

    /// <summary>An operations administrator declared an emergency; evacuation actions may be recorded.</summary>
    EmergencyDeclared,

    /// <summary>The incident was resolved in the field.</summary>
    Resolved,

    /// <summary>The incident was closed after its resolution. Terminal state.</summary>
    Closed
}

/// <summary>
///     Provides conversions between <see cref="IncidentStatus"/> values and their stable external codes.
/// </summary>
public static class IncidentStatusExtensions
{
    private static readonly Dictionary<IncidentStatus, string> Codes = new()
    {
        [IncidentStatus.Reported] = "reported",
        [IncidentStatus.Confirmed] = "confirmed",
        [IncidentStatus.Dismissed] = "dismissed",
        [IncidentStatus.EmergencyDeclared] = "emergency_declared",
        [IncidentStatus.Resolved] = "resolved",
        [IncidentStatus.Closed] = "closed"
    };

    // Codes written by the first version of the API. They are read as their current equivalent so stored rows and old
    // clients keep working; the IncidentWorkflowIntegration migration also rewrites the stored values.
    private static readonly Dictionary<string, IncidentStatus> LegacyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["inresponse"] = IncidentStatus.Confirmed,
        ["in_response"] = IncidentStatus.Confirmed
    };

    /// <summary>Converts the status into its external snake_case code.</summary>
    public static string ToCode(this IncidentStatus status) => Codes[status];

    /// <summary>Converts an external code, including the legacy in_response code, into an <see cref="IncidentStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static IncidentStatus FromCode(string code)
    {
        var value = code?.Trim();
        foreach (var (status, statusCode) in Codes)
            if (string.Equals(statusCode, value, StringComparison.OrdinalIgnoreCase)) return status;
        if (value is not null && LegacyCodes.TryGetValue(value, out var legacy)) return legacy;
        throw new ArgumentException($"Unknown incident status '{code}'.", nameof(code));
    }

    /// <summary>Returns true for the states in which the incident still requires attention.</summary>
    public static bool IsOpen(this IncidentStatus status) =>
        status is IncidentStatus.Reported or IncidentStatus.Confirmed or IncidentStatus.EmergencyDeclared;
}
