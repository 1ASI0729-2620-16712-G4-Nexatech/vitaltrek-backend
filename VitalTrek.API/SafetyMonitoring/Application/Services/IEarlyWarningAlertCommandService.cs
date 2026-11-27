using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.SafetyMonitoring.Application.Errors;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.SafetyMonitoring.Application.Services;

/// <summary>
///     Application command service interface for early warning alert write operations.
/// </summary>
public interface IEarlyWarningAlertCommandService
{
    /// <summary>
    ///     Raises an early warning alert for an active tracking session of a group the actor can operate; a session
    ///     outside that scope is reported as not found.
    /// </summary>
    Task<Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>> Handle(RaiseEarlyWarningAlertCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Acknowledges an active alert of a group the actor can operate.</summary>
    Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(AcknowledgeAlertCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Dismisses an open alert of a group the actor can operate.</summary>
    Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(DismissAlertCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Resolves an acknowledged alert of a group the actor can operate.</summary>
    Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(ResolveAlertCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>
    ///     Evaluates the delay of an active tracking session. Only the segment that leaves the furthest confirmed
    ///     checkpoint can be delayed, measured from its captured passage. A new delay raises one alert per segment; a
    ///     worse delay raises the severity of the same open alert and a closed alert is never reopened.
    /// </summary>
    /// <returns>The alerts raised or escalated by this evaluation; empty when nothing changed.</returns>
    Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>> Handle(EvaluateSessionDelaysCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Evaluates the delay of a tracking session on behalf of an authenticated requester. A session outside the
    ///     group scope of the requester, such as one whose group has another assigned guide, is reported as not found.
    /// </summary>
    /// <returns>The alerts raised or escalated by this evaluation; empty when nothing changed.</returns>
    Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>> Handle(EvaluateSessionDelaysCommand command,
        AuthenticatedActor requestedBy, CancellationToken cancellationToken);

    /// <summary>
    ///     Evaluates, on the server, the readings accepted by a persisted sync batch against the demonstration rules and
    ///     marks the batch as evaluated in the same unit of work. An evaluated batch is not evaluated again and an
    ///     evidence never raises a second alert, whatever the status of the first one.
    /// </summary>
    /// <returns>
    ///     The alerts raised or escalated by this evaluation, including the delay of an active session; empty when the
    ///     batch shows no risk or was already evaluated.
    /// </returns>
    Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>> Handle(EvaluateSyncBatchCommand command,
        CancellationToken cancellationToken);
}
