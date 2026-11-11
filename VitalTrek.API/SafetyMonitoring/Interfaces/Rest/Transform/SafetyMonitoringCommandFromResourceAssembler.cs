using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Transform;

/// <summary>
///     Assembles Safety Monitoring domain commands from REST resources. The agency always comes from the
///     authenticated user, never from the request body.
/// </summary>
public static class SafetyMonitoringCommandFromResourceAssembler
{
    /// <summary>
    ///     Transforms a <see cref="RaiseEarlyWarningAlertResource"/> into a <see cref="RaiseEarlyWarningAlertCommand"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the alert type or severity code is unknown.</exception>
    public static RaiseEarlyWarningAlertCommand ToCommandFromResource(RaiseEarlyWarningAlertResource resource,
        int agencyId) =>
        new(agencyId, resource.SessionId, resource.CheckpointId, AlertTypeExtensions.FromCode(resource.AlertType),
            AlertSeverityExtensions.FromCode(resource.Severity), resource.Description);

    /// <summary>
    ///     Transforms an <see cref="EvaluateSessionDelaysResource"/> into an <see cref="EvaluateSessionDelaysCommand"/>.
    /// </summary>
    public static EvaluateSessionDelaysCommand ToCommandFromResource(EvaluateSessionDelaysResource resource,
        int agencyId) =>
        new(agencyId, resource.SessionId);
}
