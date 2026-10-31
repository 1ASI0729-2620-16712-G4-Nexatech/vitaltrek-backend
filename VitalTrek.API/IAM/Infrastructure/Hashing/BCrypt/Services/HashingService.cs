using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using BCryptNet = BCrypt.Net.BCrypt;

namespace VitalTrek.API.IAM.Infrastructure.Hashing.BCrypt.Services;

/// <summary>
///     BCrypt implementation of <see cref="IHashingService"/>.
/// </summary>
public class HashingService : IHashingService
{
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCryptNet.Verify(password, passwordHash);
    }
}
