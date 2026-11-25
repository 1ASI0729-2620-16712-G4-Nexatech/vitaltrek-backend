using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>Assembles the cross-context actor contract from a server-authenticated user.</summary>
public static class AuthenticatedActorFromEntityAssembler
{
    /// <summary>Copies only the identifiers and role required for authorization.</summary>
    public static AuthenticatedActor ToActorFromEntity(User user) =>
        new(user.Id, user.AgencyId, user.Role.ToCode(), user.FieldGuideId, user.IdentityDocument);
}
