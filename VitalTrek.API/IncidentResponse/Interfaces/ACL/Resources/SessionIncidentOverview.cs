namespace VitalTrek.API.IncidentResponse.Interfaces.ACL.Resources;

/// <summary>
///     Counts and identifiers of the incidents of a tracking session, for the Tour Closure summary. It carries no
///     description, participant, contact or health data.
/// </summary>
/// <param name="TotalIncidents">Every incident reported during the session.</param>
/// <param name="OpenIncidents">Incidents still reported, confirmed or in a declared emergency.</param>
/// <param name="EmergencyIncidents">Incidents for which an emergency was declared at some point.</param>
/// <param name="DismissedIncidents">Incidents dismissed as false reports.</param>
/// <param name="EvacuationActions">Evacuation actions recorded for the incidents of the session.</param>
/// <param name="OpenIncidentIds">Identifiers of the open incidents, oldest first.</param>
public sealed record SessionIncidentOverview(
    int TotalIncidents,
    int OpenIncidents,
    int EmergencyIncidents,
    int DismissedIncidents,
    int EvacuationActions,
    IReadOnlyList<int> OpenIncidentIds);
