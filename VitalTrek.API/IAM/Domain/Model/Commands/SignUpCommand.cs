using VitalTrek.API.IAM.Domain.Model.ValueObjects;

namespace VitalTrek.API.IAM.Domain.Model.Commands;

/// <summary>
///     Command to register a new agency together with its first operations administrator.
/// </summary>
/// <param name="AgencyName">The commercial name of the agency.</param>
/// <param name="Name">The full name of the operations administrator.</param>
/// <param name="Username">The validated username of the administrator.</param>
/// <param name="Password">The plain password, hashed before persistence.</param>
public record SignUpCommand(string AgencyName, string Name, Username Username, string Password);
