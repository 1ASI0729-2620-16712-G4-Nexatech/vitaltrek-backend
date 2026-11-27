using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Entities;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP response resources from Incident Response aggregates. Enumerations are mapped explicitly to their
///     stable external codes.
/// </summary>
public static class IncidentResponseResourceAssembler
{
    /// <summary>Transforms an <see cref="Incident"/> into an <see cref="IncidentResource"/>.</summary>
    public static IncidentResource ToResourceFromEntity(Incident entity) =>
        new(entity.Id, entity.SessionId, entity.ExpeditionGroupId, entity.AgencyId, entity.AlertId,
            entity.ReportedByUserId, entity.ReportedAt, entity.IncidentType.ToCode(), entity.Severity.ToCode(),
            entity.Description, entity.Status.ToCode(), entity.IsOpen, entity.ConfirmedAt, entity.DismissedAt,
            entity.EmergencyDeclaredAt, entity.ResolvedAt, entity.ClosedAt,
            entity.StatusChanges.OrderBy(change => change.OccurredAt).ThenBy(change => change.Id)
                .Select(ToResourceFromEntity).ToArray());

    /// <summary>Transforms an <see cref="IncidentStatusChange"/> into an <see cref="IncidentStatusChangeResource"/>.</summary>
    public static IncidentStatusChangeResource ToResourceFromEntity(IncidentStatusChange entity) =>
        new(entity.FromStatus?.ToCode(), entity.ToStatus.ToCode(), entity.ActorUserId, entity.Note, entity.OccurredAt);

    /// <summary>Transforms an <see cref="EvacuationAction"/> into an <see cref="EvacuationActionResource"/>.</summary>
    public static EvacuationActionResource ToResourceFromEntity(EvacuationAction entity) =>
        new(entity.Id, entity.ClientActionId, entity.IncidentId, entity.ExpeditionGroupId, entity.RecordedByUserId,
            entity.Description, entity.Location, entity.Status.ToCode(), entity.RecordedAt, entity.SynchronizedAt);

    /// <summary>
    ///     Transforms an <see cref="IncidentNotification"/> into an <see cref="IncidentNotificationResource"/>. The stored
    ///     subject and body are not returned.
    /// </summary>
    public static IncidentNotificationResource ToResourceFromEntity(IncidentNotification entity) =>
        new(entity.Id, entity.NotificationId, entity.IncidentId, entity.AgencyId, entity.RequestedByUserId,
            entity.RecipientType.ToCode(), entity.RecipientAddress, entity.Channel, entity.Status.ToCode(),
            entity.RequestedAt, entity.AttemptCount, entity.LastAttemptStartedAt, entity.LastErrorCode,
            entity.ProviderMessageId, entity.AcceptedAt, entity.DeliveredAt, entity.FailedAt, entity.FailureCode);
}
