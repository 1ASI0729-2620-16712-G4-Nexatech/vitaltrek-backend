using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Application.Errors;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of the IAM command handling.
/// </summary>
public static class ActionResultFromIamResultAssembler
{
    /// <summary>
    ///     Assembles the action result of a sign-up operation.
    /// </summary>
    /// <param name="result">The application result.</param>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="getUserByIdActionName">The action used to build the location header.</param>
    /// <param name="usersControllerName">The controller that exposes the created user.</param>
    /// <returns>The HTTP action result.</returns>
    public static ActionResult ToActionResultFromSignUpResult(
        Result<User, ESignUpError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        string getUserByIdActionName,
        string usersControllerName) =>
        result switch
        {
            Result<User, ESignUpError>.Success success =>
                controller.CreatedAtAction(getUserByIdActionName, usersControllerName, new { id = success.Value.Id },
                    UserResourceFromEntityAssembler.ToResourceFromEntity(success.Value)),
            Result<User, ESignUpError>.Failure { Error: ESignUpError.DuplicateUsername } =>
                controller.Conflict(localizer["UsernameAlreadyExists"].Value),
            Result<User, ESignUpError>.Failure { Error: ESignUpError.InvalidData } =>
                controller.BadRequest(localizer["InvalidSignUpRequest"].Value),
            _ => UnexpectedError(controller, localizer, "UnexpectedErrorSigningUp")
        };

    /// <summary>
    ///     Assembles the action result of a sign-in operation.
    /// </summary>
    /// <param name="result">The application result.</param>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="localizer">The string localizer.</param>
    /// <returns>The HTTP action result.</returns>
    public static ActionResult ToActionResultFromSignInResult(
        Result<(User User, string Token), ESignInError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer) =>
        result switch
        {
            Result<(User User, string Token), ESignInError>.Success success =>
                controller.Ok(UserResourceFromEntityAssembler.ToAuthenticatedResourceFromEntity(
                    success.Value.User, success.Value.Token)),
            Result<(User User, string Token), ESignInError>.Failure { Error: ESignInError.InvalidCredentials } =>
                controller.Unauthorized(localizer["InvalidCredentials"].Value),
            _ => UnexpectedError(controller, localizer, "UnexpectedErrorSigningIn")
        };

    /// <summary>
    ///     Assembles the action result of a create user operation.
    /// </summary>
    /// <param name="result">The application result.</param>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="getUserByIdActionName">The action used to build the location header.</param>
    /// <returns>The HTTP action result.</returns>
    public static ActionResult ToActionResultFromCreateUserResult(
        Result<User, ECreateUserError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        string getUserByIdActionName) =>
        result switch
        {
            Result<User, ECreateUserError>.Success success =>
                controller.CreatedAtAction(getUserByIdActionName, new { id = success.Value.Id },
                    UserResourceFromEntityAssembler.ToResourceFromEntity(success.Value)),
            Result<User, ECreateUserError>.Failure failure => failure.Error switch
            {
                ECreateUserError.RoleNotAllowed => controller.BadRequest(localizer["UserRoleNotAllowed"].Value),
                ECreateUserError.InvalidData => controller.BadRequest(localizer["InvalidUserRequest"].Value),
                ECreateUserError.DuplicateUsername => controller.Conflict(localizer["UsernameAlreadyExists"].Value),
                ECreateUserError.LinkedIdentityAlreadyExists =>
                    controller.Conflict(localizer["LinkedIdentityAlreadyExists"].Value),
                _ => UnexpectedError(controller, localizer, "UnexpectedErrorCreatingUser")
            },
            _ => UnexpectedError(controller, localizer, "UnexpectedErrorCreatingUser")
        };

    private static ActionResult UnexpectedError(ControllerBase controller, IStringLocalizer<SharedResource> localizer,
        string detailKey) =>
        controller.Problem(
            title: localizer["UnexpectedServerError"].Value,
            detail: localizer[detailKey].Value,
            statusCode: StatusCodes.Status500InternalServerError);
}
