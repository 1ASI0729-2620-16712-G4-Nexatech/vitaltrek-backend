using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Application.Internal;

/// <summary>
///     Role policy of Incident Response, expressed with the role codes published by the IAM authenticated actor contract.
///     The assigned field guide verifies in the field (confirm, dismiss, evacuate); only an operations administrator
///     declares an emergency and requests external communications; both resolve and close.
/// </summary>
public static class IncidentActorRoles
{
    /// <summary>Role code of an operations administrator.</summary>
    public const string Administrator = "operations_administrator";

    /// <summary>Role code of a field guide.</summary>
    public const string FieldGuide = "field_guide";

    /// <summary>Returns true for staff roles that can report and read incidents.</summary>
    public static bool IsStaff(AuthenticatedActor actor) => actor.RoleCode is Administrator or FieldGuide;

    /// <summary>Returns true when the role of the actor may execute the transition that reaches <paramref name="target"/>.</summary>
    public static bool CanMoveTo(AuthenticatedActor actor, IncidentStatus target) => target switch
    {
        IncidentStatus.Confirmed or IncidentStatus.Dismissed => actor.RoleCode == FieldGuide,
        IncidentStatus.EmergencyDeclared => actor.RoleCode == Administrator,
        IncidentStatus.Resolved or IncidentStatus.Closed => IsStaff(actor),
        _ => false
    };
}
