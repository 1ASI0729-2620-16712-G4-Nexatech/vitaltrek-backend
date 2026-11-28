namespace VitalTrek.API.IncidentResponse.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the evacuation actions of an incident, ordered by capture time.
/// </summary>
public record GetEvacuationActionsByIncidentIdQuery(int AgencyId, int IncidentId);
