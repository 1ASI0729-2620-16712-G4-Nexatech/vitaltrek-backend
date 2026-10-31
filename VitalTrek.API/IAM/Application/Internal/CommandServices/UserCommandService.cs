using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.IAM.Application.Errors;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.IAM.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IUserCommandService"/>.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="agencyRepository">The agency repository.</param>
/// <param name="hashingService">The password hashing service.</param>
/// <param name="tokenService">The access token service.</param>
/// <param name="expeditionSetupContextFacade">Facade used to verify the linked field guide or manifest identity.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class UserCommandService(
    IUserRepository userRepository,
    IAgencyRepository agencyRepository,
    IHashingService hashingService,
    ITokenService tokenService,
    IExpeditionSetupContextFacade expeditionSetupContextFacade,
    IUnitOfWork unitOfWork,
    ILogger<UserCommandService> logger) : IUserCommandService
{
    /// <summary>
    ///     Minimum number of characters accepted for a password.
    /// </summary>
    public const int MinimumPasswordLength = 6;

    /// <inheritdoc />
    public async Task<Result<User, ESignUpError>> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        if (command.Password.Length < MinimumPasswordLength)
            return new Result<User, ESignUpError>.Failure(ESignUpError.InvalidData);

        if (await userRepository.ExistsByUsernameAsync(command.Username, cancellationToken))
        {
            logger.LogWarning("Sign-up rejected because username {Username} already exists", command.Username);
            return new Result<User, ESignUpError>.Failure(ESignUpError.DuplicateUsername);
        }

        Agency? agency = null;
        try
        {
            agency = new Agency(command);
            await agencyRepository.AddAsync(agency, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            var administrator = new User(command.Username, hashingService.HashPassword(command.Password),
                command.Name, Role.OperationsAdministrator, agency.Id);
            await userRepository.AddAsync(administrator, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<User, ESignUpError>.Success(administrator);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid sign-up data for username {Username}", command.Username);
            await RemoveOrphanAgencyAsync(agency, cancellationToken);
            return new Result<User, ESignUpError>.Failure(ESignUpError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            logger.LogWarning(ex, "Duplicate key violation during sign-up for username {Username}", command.Username);
            await RemoveOrphanAgencyAsync(agency, cancellationToken);
            return new Result<User, ESignUpError>.Failure(ESignUpError.DuplicateUsername);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during sign-up for username {Username}", command.Username);
            await RemoveOrphanAgencyAsync(agency, cancellationToken);
            return new Result<User, ESignUpError>.Failure(ESignUpError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<(User User, string Token), ESignInError>> Handle(SignInCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await userRepository.FindByUsernameAsync(command.Username, cancellationToken);
            if (user is null || !hashingService.VerifyPassword(command.Password, user.PasswordHash))
            {
                logger.LogWarning("Failed sign-in attempt for username {Username}", command.Username);
                return new Result<(User, string), ESignInError>.Failure(ESignInError.InvalidCredentials);
            }

            var token = tokenService.GenerateToken(user);
            return new Result<(User, string), ESignInError>.Success((user, token));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during sign-in for username {Username}", command.Username);
            return new Result<(User, string), ESignInError>.Failure(ESignInError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<User, ECreateUserError>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.Role is not (Role.FieldGuide or Role.AdventureTourist))
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.RoleNotAllowed);

        if (command.Password.Length < MinimumPasswordLength)
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.InvalidData);

        if (await userRepository.ExistsByUsernameAsync(command.Username, cancellationToken))
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.DuplicateUsername);

        var linkedIdentityRegistered = command.Role == Role.FieldGuide
            ? command.FieldGuideId is > 0 && await expeditionSetupContextFacade.ExistsFieldGuide(command.AgencyId,
                command.FieldGuideId.Value, cancellationToken)
            : !string.IsNullOrWhiteSpace(command.IdentityDocument) && await expeditionSetupContextFacade
                .ExistsManifestIdentityDocument(command.AgencyId, command.IdentityDocument.Trim(), cancellationToken);
        if (!linkedIdentityRegistered)
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.LinkedIdentityNotFound);

        var linkedIdentityExists = command.Role == Role.FieldGuide
            ? command.FieldGuideId is > 0 && await userRepository.ExistsByAgencyIdAndFieldGuideIdAsync(
                command.AgencyId, command.FieldGuideId.Value, cancellationToken)
            : !string.IsNullOrWhiteSpace(command.IdentityDocument) && await userRepository.ExistsByAgencyIdAndIdentityDocumentAsync(
                command.AgencyId, command.IdentityDocument.Trim(), cancellationToken);
        if (linkedIdentityExists)
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.LinkedIdentityAlreadyExists);

        try
        {
            var user = new User(command.Username, hashingService.HashPassword(command.Password), command.Name,
                command.Role, command.AgencyId, command.FieldGuideId, command.IdentityDocument);
            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<User, ECreateUserError>.Success(user);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data creating user {Username}", command.Username);
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            logger.LogWarning(ex, "Duplicate key violation creating user {Username}", command.Username);
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.DuplicateUsername);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating user {Username}", command.Username);
            return new Result<User, ECreateUserError>.Failure(ECreateUserError.UnexpectedError);
        }
    }

    private async Task RemoveOrphanAgencyAsync(Agency? agency, CancellationToken cancellationToken)
    {
        if (agency is null || agency.Id == 0) return;
        try
        {
            agencyRepository.Remove(agency);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not remove orphan agency {AgencyId} after a failed sign-up", agency.Id);
        }
    }
}
