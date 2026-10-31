using VitalTrek.API.IAM.Domain.Model.ValueObjects;

namespace VitalTrek.API.IAM.Domain.Model.Commands;

/// <summary>
///     Command issued by an operations administrator to create a field guide or adventure tourist account.
/// </summary>
/// <param name="AgencyId">The agency of the administrator issuing the command.</param>
/// <param name="Name">The full name of the new user.</param>
/// <param name="Username">The validated username of the new user.</param>
/// <param name="Password">The plain password, hashed before persistence.</param>
/// <param name="Role">The role of the new account.</param>
/// <param name="FieldGuideId">The field guide profile to link, required for field guides.</param>
/// <param name="IdentityDocument">The manifest identity document to link, required for adventure tourists.</param>
public record CreateUserCommand(
    int AgencyId,
    string Name,
    Username Username,
    string Password,
    Role Role,
    int? FieldGuideId,
    string? IdentityDocument);
