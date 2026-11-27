using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VitalTrek.API.SafetyMonitoring.Application.Errors;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.SafetyMonitoring.Domain.Services;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IEarlyWarningAlertCommandService"/>.
/// </summary>
/// <param name="alertRepository">The early warning alert repository.</param>
/// <param name="externalTrackingSessionService">The outbound service that validates the tracking session.</param>
/// <param name="externalRouteSetupService">The outbound service that reads the route configuration.</param>
/// <param name="externalCheckpointPassageService">The outbound service that reads the checkpoint passages.</param>
/// <param name="externalSyncBatchService">The outbound service that reads the persisted sync batches.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="timeProvider">The clock used to timestamp evaluated alerts.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class EarlyWarningAlertCommandService(
    IEarlyWarningAlertRepository alertRepository,
    ExternalTrackingSessionService externalTrackingSessionService,
    ExternalRouteSetupService externalRouteSetupService,
    ExternalCheckpointPassageService externalCheckpointPassageService,
    ExternalSyncBatchService externalSyncBatchService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
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
        ChangeStatus(command.AgencyId, command.AlertId,
            alert => alert.Acknowledge(command.UserId, timeProvider.GetUtcNow()), "acknowledging", cancellationToken);

    /// <inheritdoc />
    public Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(DismissAlertCommand command,
        CancellationToken cancellationToken) =>
        ChangeStatus(command.AgencyId, command.AlertId,
            alert => alert.Dismiss(command.UserId, timeProvider.GetUtcNow()), "dismissing", cancellationToken);

    /// <inheritdoc />
    public Task<Result<EarlyWarningAlert, EUpdateAlertStatusError>> Handle(ResolveAlertCommand command,
        CancellationToken cancellationToken) =>
        ChangeStatus(command.AgencyId, command.AlertId,
            alert => alert.Resolve(command.UserId, timeProvider.GetUtcNow()), "resolving", cancellationToken);

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

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>> Handle(
        EvaluateSyncBatchCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var batch = await externalSyncBatchService.FindBatchToEvaluate(command.AgencyId, command.BatchId,
                cancellationToken);
            if (batch is null) return SyncBatchEvaluationFail(EEvaluateSyncBatchError.BatchNotFound);
            // A batch is evaluated once; a repeated request keeps the alerts raised by the first evaluation.
            if (batch.IsEvaluated) return SyncBatchEvaluationSuccess([]);
            if (!await externalTrackingSessionService.ExistsSession(batch.AgencyId, batch.SessionId, cancellationToken))
                return SyncBatchEvaluationFail(EEvaluateSyncBatchError.SessionNotFound);
            var groupId = await externalTrackingSessionService.FindExpeditionGroupId(batch.SessionId, cancellationToken);
            if (groupId is null) return SyncBatchEvaluationFail(EEvaluateSyncBatchError.SessionNotFound);

            var baselines = await externalRouteSetupService.FindParticipantBaselines(batch.AgencyId, groupId.Value,
                cancellationToken);
            var candidates = batch.Readings
                .OrderBy(reading => reading.RecordedAt).ThenBy(reading => reading.ClientRecordId)
                .SelectMany(reading => EvaluateReading(batch, reading, baselines))
                .ToArray();
            var existingKeys = await alertRepository.FindExistingEvidenceKeysAsync(batch.SessionId,
                candidates.Select(EvidenceKeyOf).ToArray(), cancellationToken);

            // The evaluated status is staged before any alert, so nothing is tracked when the batch is not found.
            if (!await externalSyncBatchService.StageEvaluationCompleted(batch.AgencyId, batch.BatchId, cancellationToken))
                return SyncBatchEvaluationFail(EEvaluateSyncBatchError.BatchNotFound);

            var raisedAt = timeProvider.GetUtcNow();
            var stagedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var raised = new List<EarlyWarningAlert>();
            foreach (var candidate in candidates)
            {
                var evidenceKey = EvidenceKeyOf(candidate);
                // An evidence that already produced an alert, whatever its status, is never alerted again.
                if (existingKeys.Contains(evidenceKey) || !stagedKeys.Add(evidenceKey)) continue;
                var alert = new EarlyWarningAlert(candidate, groupId.Value, raisedAt);
                await alertRepository.AddAsync(alert, cancellationToken);
                raised.Add(alert);
            }

            // Alerts and the evaluated status are saved together: a failure keeps the batch pending for a retry.
            await unitOfWork.CompleteAsync(cancellationToken);
            return SyncBatchEvaluationSuccess(raised);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            logger.LogWarning(ex, "Concurrent evaluation of sync batch {BatchId}; it stays pending for a retry",
                command.BatchId);
            return SyncBatchEvaluationFail(EEvaluateSyncBatchError.ConcurrentEvaluation);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error evaluating sync batch {BatchId}", command.BatchId);
            return SyncBatchEvaluationFail(EEvaluateSyncBatchError.UnexpectedError);
        }
    }

    private List<RecordEvaluatedAlertCommand> EvaluateReading(SyncBatchToEvaluate batch, ReadingToEvaluate reading,
        IReadOnlyDictionary<int, (int BaselineMin, int BaselineMax)> baselines)
    {
        var commands = new List<RecordEvaluatedAlertCommand>();
        // A reading that the server could not attribute to a participant cannot support a participant alert.
        if (reading.MemberId is not { } memberId) return commands;
        var reference = reading.ClientRecordId.ToString();
        try
        {
            if (reading.HeartRate is { } heartRate && baselines.TryGetValue(memberId, out var baseline) &&
                DemonstrationSafetyRules.HeartRateSeverityFor(heartRate, baseline.BaselineMin, baseline.BaselineMax)
                    is { } heartRateSeverity)
                commands.Add(new RecordEvaluatedAlertCommand(batch.AgencyId, batch.SessionId, null, memberId,
                    SafetyRule.HeartRateOutsideBaseline, heartRateSeverity,
                    new AlertEvidence(heartRate, baseline.BaselineMin, baseline.BaselineMax, AlertEvidence.BeatsPerMinute,
                        reading.RecordedAt, reading.ClientRecordId),
                    reference,
                    string.Format(CultureInfo.InvariantCulture,
                        "Heart rate of {0} bpm is outside the participant baseline of {1}-{2} bpm. " +
                        "Demonstration rule, not a medical assessment.", heartRate, baseline.BaselineMin,
                        baseline.BaselineMax)));

            if (reading.DeviationMeters is { } deviationMeters &&
                DemonstrationSafetyRules.RouteDeviationSeverityFor(deviationMeters) is { } deviationSeverity)
                commands.Add(new RecordEvaluatedAlertCommand(batch.AgencyId, batch.SessionId, null, memberId,
                    SafetyRule.SimulatedRouteDeviation, deviationSeverity,
                    new AlertEvidence(Math.Round((decimal)deviationMeters, 2), null,
                        DemonstrationSafetyRules.RouteDeviationThresholdMeters, AlertEvidence.Meters, reading.RecordedAt,
                        reading.ClientRecordId, simulated: true),
                    reference,
                    string.Format(CultureInfo.InvariantCulture,
                        "Simulated route deviation of {0:0.##} m exceeds the {1} m demonstration threshold.",
                        deviationMeters, DemonstrationSafetyRules.RouteDeviationThresholdMeters)));
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            // Checkpoint Sync already rejects impossible inputs; an unexpected value is logged and never blocks the batch.
            logger.LogWarning(ex, "Reading {ClientRecordId} of sync batch {BatchId} skipped by the safety evaluation",
                reading.ClientRecordId, batch.BatchId);
        }

        return commands;
    }

    private static string EvidenceKeyOf(RecordEvaluatedAlertCommand command) =>
        EarlyWarningAlert.EvidenceKeyFor(command.Rule, command.EvidenceReference);

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

    private static Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError> SyncBatchEvaluationSuccess(
        IReadOnlyList<EarlyWarningAlert> alerts) =>
        new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>.Success(alerts);

    private static Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError> SyncBatchEvaluationFail(
        EEvaluateSyncBatchError error) =>
        new Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>.Failure(error);
}
