using VitalTrek.API.SafetyMonitoring.Application.Errors;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IEarlyWarningAlertCommandService"/>.
/// </summary>
/// <param name="alertRepository">The early warning alert repository.</param>
/// <param name="externalTrackingSessionService">The outbound service that validates the tracking session.</param>
/// <param name="externalRouteSetupService">The outbound service that reads the route configuration.</param>
/// <param name="externalCheckpointPassageService">The outbound service that reads the checkpoint passages.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class EarlyWarningAlertCommandService(
    IEarlyWarningAlertRepository alertRepository,
    ExternalTrackingSessionService externalTrackingSessionService,
    ExternalRouteSetupService externalRouteSetupService,
    ExternalCheckpointPassageService externalCheckpointPassageService,
    IUnitOfWork unitOfWork,
    ILogger<EarlyWarningAlertCommandService> logger) : IEarlyWarningAlertCommandService
{
    /// <inheritdoc />
    public async Task<Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>> Handle(RaiseEarlyWarningAlertCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await externalTrackingSessionService.ExistsSession(command.AgencyId, command.SessionId, cancellationToken))
                return Fail(ERaiseEarlyWarningAlertError.SessionNotFound);
            if (!await externalTrackingSessionService.IsSessionActive(command.SessionId, cancellationToken))
                return Fail(ERaiseEarlyWarningAlertError.SessionNotActive);
            var groupId = await externalTrackingSessionService.FindExpeditionGroupId(command.SessionId, cancellationToken);
            if (groupId is null) return Fail(ERaiseEarlyWarningAlertError.SessionNotFound);
            if (command.CheckpointId is not null && !await externalRouteSetupService.ExistsCheckpointInGroupRoute(
                    command.AgencyId, groupId.Value, command.CheckpointId.Value, cancellationToken))
                return Fail(ERaiseEarlyWarningAlertError.CheckpointNotInSessionRoute);

            var alert = new EarlyWarningAlert(command, groupId.Value, DateTimeOffset.UtcNow);
            await alertRepository.AddAsync(alert, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>.Success(alert);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data raising alert for session {SessionId}", command.SessionId);
            return Fail(ERaiseEarlyWarningAlertError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error raising alert for session {SessionId}", command.SessionId);
            return Fail(ERaiseEarlyWarningAlertError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(AcknowledgeAlertCommand command,
        CancellationToken cancellationToken) =>
        ChangeStatus(command.AgencyId, command.AlertId, alert => alert.Acknowledge(), "acknowledging", cancellationToken);

    /// <inheritdoc />
    public Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(DismissAlertCommand command,
        CancellationToken cancellationToken) =>
        ChangeStatus(command.AgencyId, command.AlertId, alert => alert.Dismiss(), "dismissing", cancellationToken);

    /// <inheritdoc />
    public Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(ResolveAlertCommand command,
        CancellationToken cancellationToken) =>
        ChangeStatus(command.AgencyId, command.AlertId, alert => alert.Resolve(), "resolving", cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>> Handle(
        EvaluateSessionDelaysCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (!await externalTrackingSessionService.ExistsSession(command.AgencyId, command.SessionId, cancellationToken))
                return EvaluationFail(EEvaluateSessionDelaysError.SessionNotFound);
            if (!await externalTrackingSessionService.IsSessionActive(command.SessionId, cancellationToken))
                return EvaluationFail(EEvaluateSessionDelaysError.SessionNotActive);
            var groupId = await externalTrackingSessionService.FindExpeditionGroupId(command.SessionId, cancellationToken);
            if (groupId is null) return EvaluationFail(EEvaluateSessionDelaysError.SessionNotFound);

            var now = DateTimeOffset.UtcNow;
            var raised = new List<EarlyWarningAlert>();
            var windows = await externalRouteSetupService.FindExpectedTimeWindows(command.AgencyId, groupId.Value,
                cancellationToken);
            foreach (var (fromCheckpointId, toCheckpointId, maximumMinutes) in windows)
            {
                var departedAt = await externalCheckpointPassageService.FindLastPassageTime(command.SessionId,
                    fromCheckpointId, cancellationToken);
                if (departedAt is null) continue;
                var arrivedAt = await externalCheckpointPassageService.FindLastPassageTime(command.SessionId,
                    toCheckpointId, cancellationToken);
                if (arrivedAt is not null) continue;

                var elapsed = now - departedAt.Value;
                var severity = EarlyWarningAlert.DelaySeverityFor(elapsed, maximumMinutes);
                if (severity is null) continue;
                if (await alertRepository.ExistsBySessionIdCheckpointIdAndTypeAsync(command.SessionId, toCheckpointId,
                        AlertType.Delay, cancellationToken))
                    continue;

                var description = $"The group has not reached checkpoint {toCheckpointId} " +
                                  $"{(int)elapsed.TotalMinutes} minutes after passing checkpoint {fromCheckpointId}. " +
                                  $"Maximum expected time: {maximumMinutes} minutes.";
                var alert = new EarlyWarningAlert(
                    new RaiseEarlyWarningAlertCommand(command.AgencyId, command.SessionId, toCheckpointId, AlertType.Delay,
                        severity.Value, description), groupId.Value, now);
                await alertRepository.AddAsync(alert, cancellationToken);
                raised.Add(alert);
            }

            if (raised.Count > 0) await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>.Success(raised);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error evaluating delays of session {SessionId}", command.SessionId);
            return EvaluationFail(EEvaluateSessionDelaysError.UnexpectedError);
        }
    }

    private async Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> ChangeStatus(int agencyId, int alertId,
        Action<EarlyWarningAlert> transition, string operation, CancellationToken cancellationToken)
    {
        var alert = await alertRepository.FindByIdAndAgencyIdAsync(alertId, agencyId, cancellationToken);
        if (alert is null)
            return new Result<EarlyWarningAlert, EUpdateAlertStatusError>.Failure(EUpdateAlertStatusError.AlertNotFound);

        try
        {
            transition(alert);
            alertRepository.Update(alert);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<EarlyWarningAlert, EUpdateAlertStatusError>.Success(alert);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid transition {Operation} alert {AlertId}", operation, alertId);
            return new Result<EarlyWarningAlert, EUpdateAlertStatusError>.Failure(EUpdateAlertStatusError.InvalidTransition);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error {Operation} alert {AlertId}", operation, alertId);
            return new Result<EarlyWarningAlert, EUpdateAlertStatusError>.Failure(EUpdateAlertStatusError.UnexpectedError);
        }
    }

    private static Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError> Fail(ERaiseEarlyWarningAlertError error) =>
        new Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>.Failure(error);

    private static Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError> EvaluationFail(
        EEvaluateSessionDelaysError error) =>
        new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>.Failure(error);
}
