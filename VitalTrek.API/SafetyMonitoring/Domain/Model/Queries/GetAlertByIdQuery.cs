namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an early warning alert of an agency.
/// </summary>
public record GetAlertByIdQuery(int AgencyId, int AlertId);
