using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP response resources from Incident Response aggregates.
/// </summary>
public static class IncidentResponseResourceAssembler
{
    /// <summary>Transforms an <see cref="Incident"/> into an <see cref="IncidentResource"/>.</summary>
    public static IncidentResource ToResourceFromEntity(Incident entity) =>
        new(entity.Id, entity.SessionId, entity.AgencyId, entity.ReportedAt, entity.IncidentType.ToCode(),
            entity.Severity.ToCode(), entity.Description, entity.Status.ToCode(), entity.ResolvedAt);

    /// <summary>
    ///     Transforms an <see cref="IncidentNotification"/> into an <see cref="IncidentNotificationResource"/>.
    /// </summary>
    public static IncidentNotificationResource ToResourceFromEntity(IncidentNotification entity) =>
        new(entity.Id, entity.IncidentId, entity.AgencyId, entity.RecipientType.ToCode(), entity.SentAt,
            entity.Channel, entity.Status.ToCode());
}
