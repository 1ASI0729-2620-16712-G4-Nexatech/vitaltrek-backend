namespace VitalTrek.API.IAM.Application.Errors;

/// <summary>
///     Error states that may occur while registering an agency and its administrator.
/// </summary>
public enum ESignUpError
{
    /// <summary>
    ///     The sign-up data violates the account or agency invariants.
    /// </summary>
    InvalidData,

    /// <summary>
    ///     The username is already registered.
    /// </summary>
    DuplicateUsername,

    /// <summary>
    ///     An unexpected persistence or system error occurred.
    /// </summary>
    UnexpectedError
}
