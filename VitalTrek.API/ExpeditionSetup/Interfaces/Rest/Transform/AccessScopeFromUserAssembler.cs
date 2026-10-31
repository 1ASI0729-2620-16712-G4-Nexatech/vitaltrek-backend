using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;

/// <summary>
///     Assembles the <see cref="AccessScope"/> of the authenticated user.
/// </summary>
public static class AccessScopeFromUserAssembler
{
    /// <summary>
    ///     Builds the access scope of a user: administrators see the agency, field guides their groups and
    ///     adventure tourists the groups where they appear in the manifest.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The access scope.</returns>
    public static AccessScope ToAccessScopeFromUser(User user) => user.Role switch
    {
        Role.OperationsAdministrator => new AccessScope(user.AgencyId),
        // A guide account without a linked profile must not see any group, so an impossible identifier is used.
        Role.FieldGuide => new AccessScope(user.AgencyId, user.FieldGuideId ?? -1),
        _ => new AccessScope(user.AgencyId, IdentityDocument: user.IdentityDocument ?? "-")
    };
}
