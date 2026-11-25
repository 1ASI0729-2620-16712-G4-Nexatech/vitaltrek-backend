namespace VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;

/// <summary>Outcome of validating and staging an expedition start, without saving changes.</summary>
public enum ExpeditionStartStatus
{
    Ready,
    GroupNotFound,
    GroupNotScheduled,
    RouteNotReady,
    ProtocolNotReady,
    ParticipantsNotReady
}
