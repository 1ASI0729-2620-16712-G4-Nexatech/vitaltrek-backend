namespace VitalTrek.API.IAM.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a user account returned to clients. It never includes the password hash.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The e-mail address of the account.</param>
/// <param name="Name">The full name of the user.</param>
/// <param name="Role">The role code.</param>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="FieldGuideId">The linked field guide profile, if any.</param>
/// <param name="IdentityDocument">The linked identity document, if any.</param>
public record UserResource(
    int Id,
    string Username,
    string Name,
    string Role,
    int AgencyId,
    int? FieldGuideId,
    string? IdentityDocument);
