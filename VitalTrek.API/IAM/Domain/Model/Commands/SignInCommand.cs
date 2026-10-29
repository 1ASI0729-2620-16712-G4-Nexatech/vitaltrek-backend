using VitalTrek.API.IAM.Domain.Model.ValueObjects;

namespace VitalTrek.API.IAM.Domain.Model.Commands;

/// <summary>
///     Command to authenticate a user with its credentials.
/// </summary>
/// <param name="Username">The validated username.</param>
/// <param name="Password">The plain password to verify.</param>
public record SignInCommand(Username Username, string Password);
