using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IAM.Interfaces.Rest.Resources;

/// <summary>
///     Resource to register a new agency and its first operations administrator.
/// </summary>
/// <param name="AgencyName">The commercial name of the agency.</param>
/// <param name="Name">The full name of the administrator.</param>
/// <param name="Username">The e-mail address used to sign in.</param>
/// <param name="Password">The password of the administrator.</param>
public record SignUpResource(
    [Required(ErrorMessage = "RequiredAgencyName")] string AgencyName,
    [Required(ErrorMessage = "RequiredName")] string Name,
    [Required(ErrorMessage = "RequiredUsername")] string Username,
    [Required(ErrorMessage = "RequiredPassword")]
    [MinLength(6, ErrorMessage = "PasswordMinLength")]
    string Password);
