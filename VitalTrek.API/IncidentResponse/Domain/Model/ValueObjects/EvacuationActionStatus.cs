namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     State of an evacuation action at the moment the field guide captured it.
/// </summary>
public enum EvacuationActionStatus
{
    /// <summary>The evacuation step started and is still in progress.</summary>
    InProgress,

    /// <summary>The evacuation step was completed.</summary>
    Completed,

    /// <summary>The evacuation step was aborted, for example because the route became unsafe.</summary>
    Aborted
}

/// <summary>
///     Provides conversions between <see cref="EvacuationActionStatus"/> values and their external codes.
/// </summary>
public static class EvacuationActionStatusExtensions
{
    private static readonly Dictionary<EvacuationActionStatus, string> Codes = new()
    {
        [EvacuationActionStatus.InProgress] = "in_progress",
        [EvacuationActionStatus.Completed] = "completed",
        [EvacuationActionStatus.Aborted] = "aborted"
    };

    /// <summary>Converts the status into its external snake_case code.</summary>
    public static string ToCode(this EvacuationActionStatus status) => Codes[status];

    /// <summary>Converts an external code into an <see cref="EvacuationActionStatus"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static EvacuationActionStatus FromCode(string code)
    {
        foreach (var (status, value) in Codes)
            if (string.Equals(value, code?.Trim(), StringComparison.OrdinalIgnoreCase)) return status;
        throw new ArgumentException($"Unknown evacuation action status '{code}'.", nameof(code));
    }
}
