namespace VitalTrek.API.IAM.Application.Errors;

/// <summary>
///     Error states that may occur while authenticating a user.
/// </summary>
public enum ESignInError
{
    /// <summary>
    ///     The username does not exist or the password does not match.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    ///     An unexpected system error occurred.
    /// </summary>
    UnexpectedError
}
