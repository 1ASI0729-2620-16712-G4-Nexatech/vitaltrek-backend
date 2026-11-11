namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the open (active or acknowledged) early warning alerts of an agency.
/// </summary>
public record GetActiveAlertsByAgencyIdQuery(int AgencyId);
