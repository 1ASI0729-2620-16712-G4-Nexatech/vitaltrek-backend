using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="CreateUserCommand"/> instances from REST resources.
/// </summary>
public static class CreateUserCommandFromResourceAssembler
{
    /// <summary>
    ///     Transforms a <see cref="CreateUserResource"/> into a <see cref="CreateUserCommand"/>.
    /// </summary>
    /// <param name="resource">The create user resource.</param>
    /// <param name="agencyId">The agency of the authenticated administrator.</param>
    /// <returns>The create user command.</returns>
    public static CreateUserCommand ToCommandFromResource(CreateUserResource resource, int agencyId) =>
        new(agencyId, resource.Name, new Username(resource.Username), resource.Password,
            RoleExtensions.FromCode(resource.Role), resource.FieldGuideId, resource.IdentityDocument);
}
