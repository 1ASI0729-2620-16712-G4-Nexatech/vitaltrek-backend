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
    /// <summary>Raises an early warning alert for an active tracking session.</summary>
    Task<Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>> Handle(RaiseEarlyWarningAlertCommand command,
        CancellationToken cancellationToken);

    /// <summary>Acknowledges an active alert.</summary>
    Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(AcknowledgeAlertCommand command,
        CancellationToken cancellationToken);

    /// <summary>Dismisses an open alert.</summary>
    Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(DismissAlertCommand command,
        CancellationToken cancellationToken);

    /// <summary>Resolves an acknowledged alert.</summary>
    Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(ResolveAlertCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Evaluates the delays of an active tracking session and raises one delay alert per overdue segment.
    ///     The operation is idempotent: a segment that already has a delay alert is not alerted again.
    /// </summary>
    /// <returns>The alerts raised by this evaluation; empty when no new delay was detected.</returns>
    Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>> Handle(EvaluateSessionDelaysCommand command,
        CancellationToken cancellationToken);
}
