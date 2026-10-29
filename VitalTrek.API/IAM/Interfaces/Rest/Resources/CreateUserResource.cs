using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.IAM.Interfaces.Rest.Resources;

/// <summary>
///     Resource used by an operations administrator to create a field guide or adventure tourist account.
/// </summary>
/// <param name="Name">The full name of the user.</param>
/// <param name="Username">The e-mail address used to sign in.</param>
/// <param name="Password">The initial password.</param>
/// <param name="Role">The role code: <c>field_guide</c> or <c>adventure_tourist</c>.</param>
/// <param name="FieldGuideId">The field guide profile to link. Required for field guides.</param>
/// <param name="IdentityDocument">The manifest identity document to link. Required for adventure tourists.</param>
public record CreateUserResource(
    [Required(ErrorMessage = "RequiredName")] string Name,
    [Required(ErrorMessage = "RequiredUsername")] string Username,
    [Required(ErrorMessage = "RequiredPassword")]
    [MinLength(6, ErrorMessage = "PasswordMinLength")]
    string Password,
    [Required(ErrorMessage = "RequiredRole")] string Role,
    int? FieldGuideId,
    string? IdentityDocument);
