using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP response resources from Safety Monitoring aggregates.
/// </summary>
public static class SafetyMonitoringResourceAssembler
{
    /// <summary>Transforms an <see cref="EarlyWarningAlert"/> into an <see cref="EarlyWarningAlertResource"/>.</summary>
    public static EarlyWarningAlertResource ToResourceFromEntity(EarlyWarningAlert entity) =>
        new(entity.Id, entity.SessionId, entity.ExpeditionGroupId, entity.AgencyId, entity.CheckpointId,
            entity.AlertType.ToCode(), entity.Severity.ToCode(), entity.Status.ToCode(), entity.Description,
            entity.RaisedAt);
}
