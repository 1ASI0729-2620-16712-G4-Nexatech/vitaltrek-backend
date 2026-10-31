namespace VitalTrek.API.Shared.Domain.Model;

/// <summary>
///     Reusable guard clauses that protect aggregate invariants.
/// </summary>
public static class DomainGuard
{
    /// <summary>
    ///     Ensures a text value is present and within the maximum length, returning it trimmed.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="parameterName">The parameter name reported in the exception.</param>
    /// <returns>The trimmed value.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is empty or too long.</exception>
    public static string RequireText(string? value, int maxLength, string parameterName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        return trimmed;
    }

    /// <summary>
    ///     Ensures an identifier or quantity is greater than zero.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The parameter name reported in the exception.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not positive.</exception>
    public static int RequirePositive(int value, string parameterName) =>
        value > 0 ? value : throw new ArgumentException($"{parameterName} must be greater than zero.", parameterName);
}
