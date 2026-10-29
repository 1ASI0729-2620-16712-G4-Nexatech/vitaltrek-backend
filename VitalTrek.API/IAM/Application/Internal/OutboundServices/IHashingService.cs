namespace VitalTrek.API.IAM.Application.Internal.OutboundServices;

/// <summary>
///     Outbound service contract for one-way password hashing.
/// </summary>
public interface IHashingService
{
    /// <summary>
    ///     Produces a salted hash of a plain password.
    /// </summary>
    /// <param name="password">The plain password.</param>
    /// <returns>The salted hash.</returns>
    string HashPassword(string password);

    /// <summary>
    ///     Verifies a plain password against a stored hash.
    /// </summary>
    /// <param name="password">The plain password.</param>
    /// <param name="passwordHash">The stored hash.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    bool VerifyPassword(string password, string passwordHash);
}
