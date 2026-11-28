namespace VitalTrek.API.IAM.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes IAM information to other bounded contexts
///     without sharing the IAM domain model.
/// </summary>
public interface IIamContextFacade
{
    /// <summary>
    ///     Gets the agency identifier of a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The agency identifier, or 0 when the user does not exist.</returns>
    Task<int> FetchAgencyIdByUserId(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the field guide profile linked to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The field guide identifier, or <see langword="null"/> when the user is not a field guide.</returns>
    Task<int?> FetchFieldGuideIdByUserId(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the identity document linked to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The identity document, or <see langword="null"/> when the user is not an adventure tourist.</returns>
    Task<string?> FetchIdentityDocumentByUserId(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether an e-mail address is the username of a staff account (operations administrator or field
    ///     guide) of an agency. Incident Response uses it to authorize notification recipients; it exposes no account data.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="email">The e-mail address to check.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when a staff account of the agency uses that address.</returns>
    Task<bool> IsAgencyStaffEmail(int agencyId, string email, CancellationToken cancellationToken = default);
}
