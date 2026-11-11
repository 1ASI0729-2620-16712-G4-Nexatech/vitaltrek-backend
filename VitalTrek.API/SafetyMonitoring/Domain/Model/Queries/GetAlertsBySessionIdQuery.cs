namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the early warning alerts of a tracking session.
/// </summary>
public record GetAlertsBySessionIdQuery(int AgencyId, int SessionId);
