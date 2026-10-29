using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IAM.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="User"/> aggregates.
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    ///     Finds a user by its username.
    /// </summary>
    /// <param name="username">The username to search.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The matching user if found; otherwise, <see langword="null"/>.</returns>
    Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a username is already registered.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the username exists.</returns>
    Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds all the users that belong to an agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The users of the agency.</returns>
    Task<IEnumerable<User>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a field guide profile is already linked to an account in the agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="fieldGuideId">The field guide profile identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the profile is already linked.</returns>
    Task<bool> ExistsByAgencyIdAndFieldGuideIdAsync(int agencyId, int fieldGuideId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether an identity document is already linked to an account in the agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="identityDocument">The identity document.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the document is already linked.</returns>
    Task<bool> ExistsByAgencyIdAndIdentityDocumentAsync(int agencyId, string identityDocument, CancellationToken cancellationToken = default);
}
