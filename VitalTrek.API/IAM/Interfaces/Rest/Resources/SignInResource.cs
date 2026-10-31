using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IAM.Interfaces.Rest.Resources;

/// <summary>
///     Resource with the credentials used to sign in.
/// </summary>
/// <param name="Username">The e-mail address of the account.</param>
/// <param name="Password">The password of the account.</param>
public record SignInResource(
    [Required(ErrorMessage = "RequiredUsername")] string Username,
    [Required(ErrorMessage = "RequiredPassword")] string Password);
