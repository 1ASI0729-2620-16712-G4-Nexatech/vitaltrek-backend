using VitalTrek.API.IAM.Application.Errors;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IAM.Application.Services;

/// <summary>
///     Application command service interface for user account write operations.
/// </summary>
public interface IUserCommandService
{
    /// <summary>
    ///     Registers a new agency and its first operations administrator.
    /// </summary>
    /// <param name="command">The sign-up command.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The created administrator on success, or an error on failure.</returns>
    Task<Result<User, ESignUpError>> Handle(SignUpCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Authenticates a user and issues an access token.
    /// </summary>
    /// <param name="command">The sign-in command.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The authenticated user and its token on success, or an error on failure.</returns>
    Task<Result<(User User, string Token), ESignInError>> Handle(SignInCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Creates a field guide or adventure tourist account in the administrator's agency.
    /// </summary>
    /// <param name="command">The create user command.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The created user on success, or an error on failure.</returns>
    Task<Result<User, ECreateUserError>> Handle(CreateUserCommand command, CancellationToken cancellationToken);
}
