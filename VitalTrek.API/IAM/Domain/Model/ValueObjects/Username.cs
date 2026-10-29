using System.Net.Mail;

namespace VitalTrek.API.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Value object representing the e-mail address used as the username of an account.
/// </summary>
public sealed record Username
{
    private const int MaxLength = 120;

    /// <summary>
    ///     Gets the normalized (trimmed, lowercase) username value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Username"/> value object.
    /// </summary>
    /// <param name="value">The raw username string.</param>
    /// <exception cref="ArgumentException">Thrown when the value is empty, too long or not a valid e-mail address.</exception>
    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Username cannot be null, empty, or whitespace.", nameof(value));
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Username cannot exceed {MaxLength} characters.", nameof(value));
        if (!MailAddress.TryCreate(normalized, out var address) || address.Address != normalized)
            throw new ArgumentException("Username must be a valid e-mail address.", nameof(value));
        Value = normalized;
    }

    /// <summary>
    ///     Returns the underlying username value.
    /// </summary>
    /// <returns>The username string.</returns>
    public override string ToString() => Value;
}
