using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;
using VitalTrek.API.IAM.Interfaces.Rest.Transform;
using VitalTrek.API.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest;

/// <summary>
///     Authentication controller for agency registration and sign-in.
/// </summary>
/// <param name="userCommandService">The user command service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Authentication")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<AuthenticationController> logger) : ControllerBase
{
    /// <summary>
    ///     Registers a new agency and its first operations administrator.
    /// </summary>
    /// <param name="resource">The sign-up resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created administrator account.</returns>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SignUp([FromBody] SignUpResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await userCommandService.Handle(command, cancellationToken);
            return ActionResultFromIamResultAssembler.ToActionResultFromSignUpResult(result, this, localizer,
                nameof(UsersController.GetUserById), "Users");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while signing up");
            return BadRequest(localizer["InvalidSignUpRequest"].Value);
        }
    }

    /// <summary>
    ///     Authenticates a user and returns its profile with a bearer token.
    /// </summary>
    /// <param name="resource">The sign-in resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The authenticated user with its token.</returns>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SignIn([FromBody] SignInResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = SignInCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await userCommandService.Handle(command, cancellationToken);
            return ActionResultFromIamResultAssembler.ToActionResultFromSignInResult(result, this, localizer);
        }
        catch (ArgumentException)
        {
            return Unauthorized(localizer["InvalidCredentials"].Value);
        }
    }
}
