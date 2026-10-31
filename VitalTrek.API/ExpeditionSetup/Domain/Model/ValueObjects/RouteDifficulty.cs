namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Difficulty level of a route.
/// </summary>
public enum RouteDifficulty
{
    /// <summary>Suitable for tourists without previous experience.</summary>
    Beginner,

    /// <summary>Requires basic physical preparation.</summary>
    Intermediate,

    /// <summary>Requires previous trekking experience and acclimatization.</summary>
    Advanced,

    /// <summary>Requires high-altitude experience and specialized preparation.</summary>
    Expert
}

/// <summary>
///     Provides conversions between <see cref="RouteDifficulty"/> values and their external codes.
/// </summary>
public static class RouteDifficultyExtensions
{
    /// <summary>
    ///     Converts an external code into a <see cref="RouteDifficulty"/>.
    /// </summary>
    /// <param name="code">The external code, e.g. <c>Advanced</c>.</param>
    /// <returns>The matching difficulty.</returns>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static RouteDifficulty FromCode(string code) =>
        Enum.TryParse<RouteDifficulty>(code?.Trim(), true, out var difficulty) && Enum.IsDefined(difficulty)
            ? difficulty
            : throw new ArgumentException($"Unknown route difficulty '{code}'.", nameof(code));
}
