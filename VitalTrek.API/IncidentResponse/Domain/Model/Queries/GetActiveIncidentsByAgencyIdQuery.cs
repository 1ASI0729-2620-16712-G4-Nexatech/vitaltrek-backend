namespace VitalTrek.API.IncidentResponse.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the incidents of an agency that are still open.
/// </summary>
public record GetActiveIncidentsByAgencyIdQuery(int AgencyId);
