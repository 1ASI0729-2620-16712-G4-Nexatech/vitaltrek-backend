namespace VitalTrek.API.IncidentResponse.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an incident that belongs to an agency.
/// </summary>
public record GetIncidentByIdQuery(int AgencyId, int IncidentId);
