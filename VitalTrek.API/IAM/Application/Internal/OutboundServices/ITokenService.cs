using VitalTrek.API.IAM.Domain.Model.Aggregates;

namespace VitalTrek.API.IAM.Application.Internal.OutboundServices;

/// <summary>
///     Outbound service contract for issuing and validating access tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    ///     Issues a signed access token for the user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The signed token.</returns>
    string GenerateToken(User user);

    /// <summary>
    ///     Validates an access token and extracts the user identifier.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>The user identifier when the token is valid; otherwise, <see langword="null"/>.</returns>
    Task<int?> ValidateToken(string token);
}
