namespace VitalTrek.API.IAM.Application.Errors;

/// <summary>
///     Error states that may occur while an administrator creates a user account.
/// </summary>
public enum ECreateUserError
{
    /// <summary>
    ///     The requested role cannot be created by an administrator.
    /// </summary>
    RoleNotAllowed,

    /// <summary>
    ///     The account data violates the account invariants.
    /// </summary>
    InvalidData,

    /// <summary>
    ///     The username is already registered.
    /// </summary>
    DuplicateUsername,

    /// <summary>
    ///     The field guide profile or identity document is already linked to another account of the agency.
    /// </summary>
    LinkedIdentityAlreadyExists,

    /// <summary>
    ///     The field guide profile or identity document does not exist in the agency.
    /// </summary>
    LinkedIdentityNotFound,

    /// <summary>
    ///     An unexpected persistence or system error occurred.
    /// </summary>
    UnexpectedError
}
