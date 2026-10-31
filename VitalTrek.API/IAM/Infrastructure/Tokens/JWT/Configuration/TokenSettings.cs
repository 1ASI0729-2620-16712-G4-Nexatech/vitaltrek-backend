namespace VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Configuration;

/// <summary>
///     Settings used to sign and validate JSON Web Tokens, bound from the <c>TokenSettings</c> configuration section.
/// </summary>
public class TokenSettings
{
    /// <summary>
    ///     Gets or sets the symmetric signing secret. It must contain at least 32 characters.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the token lifetime in hours.
    /// </summary>
    public int ExpirationHours { get; set; } = 12;
}
