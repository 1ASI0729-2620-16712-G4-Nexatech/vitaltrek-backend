namespace VitalTrek.API.Tests.Support;

/// <summary>
///     Time provider whose current time is set by the test, so time-dependent rules are evaluated deterministically.
/// </summary>
/// <param name="utcNow">The initial current time.</param>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Gets or sets the current time returned by the provider.</summary>
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow;
}
