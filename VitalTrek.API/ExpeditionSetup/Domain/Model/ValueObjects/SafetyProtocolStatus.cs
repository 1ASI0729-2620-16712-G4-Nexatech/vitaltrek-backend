namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Status of a safety protocol.
/// </summary>
public enum SafetyProtocolStatus
{
    /// <summary>The protocol applies to the route.</summary>
    Active,

    /// <summary>The protocol is kept for history but no longer applies.</summary>
    Inactive
}

/// <summary>
///     Provides conversions between <see cref="SafetyProtocolStatus"/> values and their external codes.
/// </summary>
public static class SafetyProtocolStatusExtensions
{
    /// <summary>
    ///     Converts the status into its external lowercase code.
    /// </summary>
    /// <param name="status">The protocol status.</param>
    /// <returns>The external code.</returns>
    public static string ToCode(this SafetyProtocolStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>
    ///     Converts an external code into a <see cref="SafetyProtocolStatus"/>.
    /// </summary>
    /// <param name="code">The external code.</param>
    /// <returns>The matching status.</returns>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static SafetyProtocolStatus FromCode(string code) =>
        Enum.TryParse<SafetyProtocolStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown safety protocol status '{code}'.", nameof(code));
}
