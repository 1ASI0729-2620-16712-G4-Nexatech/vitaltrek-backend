namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Configuration status of a route.
/// </summary>
public enum RouteStatus
{
    /// <summary>
    ///     The route is being configured and cannot be used to plan tours.
    /// </summary>
    Draft,

    /// <summary>
    ///     The route has at least one checkpoint and can be used to plan tours.
    /// </summary>
    Enabled
}

/// <summary>
///     Provides conversions between <see cref="RouteStatus"/> values and their external codes.
/// </summary>
public static class RouteStatusExtensions
{
    /// <summary>
    ///     Converts the status into its external lowercase code.
    /// </summary>
    /// <param name="status">The route status.</param>
    /// <returns>The external code.</returns>
    public static string ToCode(this RouteStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>
    ///     Converts an external code into a <see cref="RouteStatus"/>.
    /// </summary>
    /// <param name="code">The external code.</param>
    /// <returns>The matching status.</returns>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static RouteStatus FromCode(string code) =>
        Enum.TryParse<RouteStatus>(code?.Trim(), true, out var status) && Enum.IsDefined(status)
            ? status
            : throw new ArgumentException($"Unknown route status '{code}'.", nameof(code));
}
