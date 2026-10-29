using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Configuration;

namespace VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Services;

/// <summary>
///     JSON Web Token implementation of <see cref="ITokenService"/>.
/// </summary>
/// <param name="tokenSettings">The token settings.</param>
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    /// <summary>
    ///     Claim type that carries the agency identifier of the user.
    /// </summary>
    public const string AgencyIdClaimType = "agency_id";

    private readonly TokenSettings _settings = tokenSettings.Value;

    private SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(_settings.Secret));

    /// <inheritdoc />
    public string GenerateToken(User user)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username.Value),
                new Claim(ClaimTypes.Role, user.Role.ToCode()),
                new Claim(AgencyIdClaimType, user.AgencyId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(_settings.ExpirationHours),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature)
        };
        return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
    }

    /// <inheritdoc />
    public async Task<int?> ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        });
        if (!result.IsValid || result.SecurityToken is not JsonWebToken jsonWebToken) return null;
        var userIdClaim = jsonWebToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Sid)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
