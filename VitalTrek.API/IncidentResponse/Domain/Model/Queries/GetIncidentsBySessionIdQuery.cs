namespace VitalTrek.API.IncidentResponse.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the incidents declared during a tracking session.
/// </summary>
public record GetIncidentsBySessionIdQuery(int AgencyId, int SessionId);
