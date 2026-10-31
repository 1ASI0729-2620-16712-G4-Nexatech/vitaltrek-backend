using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>
///     Assembles user resources from <see cref="User"/> aggregates.
/// </summary>
public static class UserResourceFromEntityAssembler
{
    /// <summary>
    ///     Transforms a <see cref="User"/> into a <see cref="UserResource"/>.
    /// </summary>
    /// <param name="entity">The user aggregate.</param>
    /// <returns>The user resource.</returns>
    public static UserResource ToResourceFromEntity(User entity) =>
        new(entity.Id, entity.Username.Value, entity.Name, entity.Role.ToCode(), entity.AgencyId,
            entity.FieldGuideId, entity.IdentityDocument);

    /// <summary>
    ///     Transforms a <see cref="User"/> and its token into an <see cref="AuthenticatedUserResource"/>.
    /// </summary>
    /// <param name="entity">The authenticated user.</param>
    /// <param name="token">The issued token.</param>
    /// <returns>The authenticated user resource.</returns>
    public static AuthenticatedUserResource ToAuthenticatedResourceFromEntity(User entity, string token) =>
        new(entity.Id, entity.Username.Value, entity.Name, entity.Role.ToCode(), entity.AgencyId,
            entity.FieldGuideId, entity.IdentityDocument, token);
}
