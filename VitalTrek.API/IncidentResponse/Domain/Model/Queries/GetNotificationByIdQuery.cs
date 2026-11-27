namespace VitalTrek.API.IncidentResponse.Domain.Model.Queries;

/// <summary>
///     Query to retrieve one notification of an incident by its stable identity.
/// </summary>
public record GetNotificationByIdQuery(int AgencyId, int IncidentId, Guid NotificationId);
