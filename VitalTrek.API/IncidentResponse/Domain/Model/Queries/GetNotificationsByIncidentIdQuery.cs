namespace VitalTrek.API.IncidentResponse.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the notifications dispatched for an incident.
/// </summary>
public record GetNotificationsByIncidentIdQuery(int AgencyId, int IncidentId);
