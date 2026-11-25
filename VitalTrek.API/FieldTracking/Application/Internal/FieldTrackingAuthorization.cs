using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Internal;

/// <summary>
///     Defines the roles allowed to access raw operational tracking data.
///     Group visibility is checked separately through the Expedition Setup facade.
/// </summary>
internal static class FieldTrackingAuthorization
{
    public static bool CanRead(AuthenticatedActor actor, int agencyId) =>
        actor.UserId > 0 && actor.AgencyId > 0 && actor.AgencyId == agencyId &&
        (actor.RoleCode == "operations_administrator" || CanWrite(actor, agencyId));

    public static bool CanWrite(AuthenticatedActor actor, int agencyId) =>
        actor.UserId > 0 && actor.AgencyId > 0 && actor.AgencyId == agencyId &&
        actor.RoleCode == "field_guide" && actor.FieldGuideId is > 0;
}
