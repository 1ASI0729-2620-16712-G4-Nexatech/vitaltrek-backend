using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;
using VitalTrek.API.IAM.Interfaces.Rest.Transform;
using VitalTrek.API.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest;

/// <summary>
///     Users controller. Every operation is scoped to the agency of the authenticated user.
/// </summary>
/// <param name="userCommandService">The user command service.</param>
/// <param name="userQueryService">The user query service.</param>
/// <param name="localizer">String localizer for response messages.</param>
/// <param name="logger">Logger for diagnostics.</param>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Users")]
[Authorize]
public class UsersController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    IStringLocalizer<SharedResource> localizer,
    ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>
    ///     Gets all the user accounts of the administrator's agency.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The users of the agency.</returns>
    [HttpGet]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(IEnumerable<UserResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var currentUser = HttpContext.GetAuthenticatedUser()!;
        var users = await userQueryService.Handle(new GetAllUsersByAgencyIdQuery(currentUser.AgencyId), cancellationToken);
        return Ok(users.Select(UserResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Gets a user account by its identifier. Administrators can read the accounts of their agency;
    ///     any other user can only read its own account.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The user account.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetUserById(int id, CancellationToken cancellationToken)
    {
        var currentUser = HttpContext.GetAuthenticatedUser()!;
        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        // Accounts of other agencies are reported as not found so their existence is not revealed.
        if (user is null || (user.Id != currentUser.Id && !currentUser.IsAdministratorOf(user.AgencyId)))
            return NotFound(localizer["UserNotFound"].Value);
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    /// <summary>
    ///     Creates a field guide or adventure tourist account in the administrator's agency.
    /// </summary>
    /// <param name="resource">The create user resource.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The created user account.</returns>
    [HttpPost]
    [Authorize(Role.OperationsAdministrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateUser([FromBody] CreateUserResource resource, CancellationToken cancellationToken)
    {
        var currentUser = HttpContext.GetAuthenticatedUser()!;
        try
        {
            var command = CreateUserCommandFromResourceAssembler.ToCommandFromResource(resource, currentUser.AgencyId);
            var result = await userCommandService.Handle(command, cancellationToken);
            return ActionResultFromIamResultAssembler.ToActionResultFromCreateUserResult(result, this, localizer,
                nameof(GetUserById));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating a user in agency {AgencyId}", currentUser.AgencyId);
            return BadRequest(localizer["InvalidUserRequest"].Value);
        }
    }
}
