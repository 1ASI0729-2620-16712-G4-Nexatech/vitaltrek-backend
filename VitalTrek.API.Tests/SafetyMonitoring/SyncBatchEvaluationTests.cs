using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.SafetyMonitoring.Application.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Errors;
using VitalTrek.API.SafetyMonitoring.Application.Internal.CommandServices;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring;

/// <summary>
///     D01: the server evaluates the readings accepted by a persisted sync batch. Alerts keep the original capture time,
///     the batch and its alerts are saved together and a repeated evaluation never duplicates or reopens an alert.
///     The thresholds are demonstration rules, not a medical recommendation.
/// </summary>
public sealed class SyncBatchEvaluationTests
{
    private const int Agency = 1, Session = 7, Group = 5, Batch = 40, Member = 60, MemberWithoutBaseline = 99;
    private static readonly DateTimeOffset CapturedAt = new(2026, 11, 26, 9, 30, 0, TimeSpan.FromHours(-5));
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

    private readonly List<EarlyWarningAlert> _savedAlerts = [];
    private readonly List<EarlyWarningAlert> _pendingAlerts = [];
    private TelemetryReadingSnapshot[] _readings = [];
    private string _evaluationStatus = "pending";
    private bool _evaluationStaged;
    private bool _sessionActive;
    private int _saves;
    private Exception? _saveFailure;

    [Fact]
    public async Task NormalBatchRaisesNoAlertAndIsSavedAsEvaluatedOnce()
    {
        _readings = [Reading(Guid.NewGuid())];

        var alerts = Success(await Evaluate());

        Assert.Empty(alerts);
        Assert.Equal(1, _saves);
        Assert.Equal("evaluated", _evaluationStatus);
    }

    [Fact]
    public async Task AbnormalHeartRateRaisesOneAlertThatKeepsTheOriginalCaptureTime()
    {
        var readingId = Guid.NewGuid();
        _readings = [Reading(readingId, heartRate: 160)];

        var alert = Assert.Single(Success(await Evaluate()));

        Assert.Same(alert, Assert.Single(_savedAlerts));
        Assert.Equal(SafetyRule.HeartRateOutsideBaseline, alert.Rule);
        Assert.Equal(AlertType.VitalSignAnomaly, alert.AlertType);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal(Agency, alert.AgencyId);
        Assert.Equal(Session, alert.SessionId);
        Assert.Equal(Group, alert.ExpeditionGroupId);
        Assert.Equal(Member, alert.MemberId);
        Assert.Equal($"heart_rate_outside_baseline:{readingId}", alert.EvidenceKey);
        Assert.Equal(new AlertEvidence(160, 50, 140, AlertEvidence.BeatsPerMinute, CapturedAt, readingId), alert.Evidence);
        Assert.Equal(Now, alert.RaisedAt);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task SimulatedDeviationRaisesARouteDeviationAlertMarkedAsSimulated()
    {
        _readings = [Reading(Guid.NewGuid(), deviation: 150)];

        var alert = Assert.Single(Success(await Evaluate()));

        Assert.Equal(SafetyRule.SimulatedRouteDeviation, alert.Rule);
        Assert.Equal(AlertType.RouteDeviation, alert.AlertType);
        Assert.True(alert.Evidence!.Simulated);
        Assert.Equal(150m, alert.Evidence.ObservedValue);
        Assert.Equal(100m, alert.Evidence.ExpectedMaximum);
    }

    [Fact]
    public async Task ReadingWithBothRisksRaisesOneAlertPerRule()
    {
        _readings = [Reading(Guid.NewGuid(), heartRate: 160, deviation: 150)];

        var alerts = Success(await Evaluate());

        Assert.Equal(2, alerts.Count);
        Assert.Equal(2, alerts.Select(alert => alert.EvidenceKey).Distinct().Count());
    }

    [Fact]
    public async Task EvaluatedBatchIsNotEvaluatedAgain()
    {
        _readings = [Reading(Guid.NewGuid(), heartRate: 160)];
        Success(await Evaluate());

        var repeated = Success(await Evaluate());

        Assert.Empty(repeated);
        Assert.Single(_savedAlerts);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task EvidenceThatAlreadyRaisedAnAlertIsNeverAlertedAgainEvenWhenClosed()
    {
        _readings = [Reading(Guid.NewGuid(), heartRate: 160)];
        Success(await Evaluate());
        _savedAlerts[0].Dismiss(9, Now);
        // A retry still sees the batch pending, for example after a concurrent evaluation.
        _evaluationStatus = "pending";

        var repeated = Success(await Evaluate());

        Assert.Empty(repeated);
        var alert = Assert.Single(_savedAlerts);
        Assert.Equal(AlertStatus.Dismissed, alert.Status);
        Assert.Equal("evaluated", _evaluationStatus);
    }

    [Fact]
    public async Task FailureBeforeCommitKeepsTheBatchPendingAndARetryRecoversIt()
    {
        _readings = [Reading(Guid.NewGuid(), heartRate: 160)];
        _saveFailure = new InvalidOperationException("Simulated storage failure before commit.");

        var failed = await Evaluate();

        Assert.Equal(EEvaluateSyncBatchError.UnexpectedError, Assert.IsType<Result<IReadOnlyList<EarlyWarningAlert>,
            EEvaluateSyncBatchError>.Failure>(failed).Error);
        Assert.Empty(_savedAlerts);
        Assert.Equal("pending", _evaluationStatus);

        _saveFailure = null;
        Assert.Single(Success(await Evaluate()));
        Assert.Single(_savedAlerts);
        Assert.Equal("evaluated", _evaluationStatus);
    }

    [Fact]
    public async Task UnattributedReadingsAndMissingBaselinesNeverBlockTheBatch()
    {
        _readings =
        [
            Reading(Guid.NewGuid(), heartRate: 200, deviation: 150, member: null),
            Reading(Guid.NewGuid(), heartRate: 200, member: MemberWithoutBaseline),
            Reading(Guid.NewGuid(), deviation: 150, member: MemberWithoutBaseline)
        ];

        var alert = Assert.Single(Success(await Evaluate()));

        // The deviation rule does not depend on the baseline; the heart rate rule needs it.
        Assert.Equal(SafetyRule.SimulatedRouteDeviation, alert.Rule);
        Assert.Equal(MemberWithoutBaseline, alert.MemberId);
        Assert.Equal("evaluated", _evaluationStatus);
    }

    [Fact]
    public async Task BatchOutsideTheAgencyIsNotFoundAndNothingIsSaved()
    {
        _readings = [Reading(Guid.NewGuid(), heartRate: 160)];

        var result = await Evaluate(agencyId: 2);

        Assert.Equal(EEvaluateSyncBatchError.BatchNotFound, Assert.IsType<Result<IReadOnlyList<EarlyWarningAlert>,
            EEvaluateSyncBatchError>.Failure>(result).Error);
        Assert.False(_evaluationStaged);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task ActiveSessionEvaluatesTheNextDelayInTheSameSave()
    {
        _sessionActive = true;
        _readings = [Reading(Guid.NewGuid(), heartRate: 160)];

        var alerts = Success(await Evaluate());

        Assert.Equal(2, alerts.Count);
        var delay = Assert.Single(alerts, alert => alert.Rule == SafetyRule.CheckpointDelay);
        Assert.Equal(4, delay.CheckpointId);
        Assert.Equal(AlertSeverity.Critical, delay.Severity);
        Assert.Equal(CapturedAt.AddHours(-2), delay.Evidence!.RecordedAt);
        Assert.Equal(1, _saves);
        Assert.Equal(2, _savedAlerts.Count);
    }

    [Fact]
    public async Task FacadePortReportsWhetherTheBatchIsEvaluated()
    {
        _readings = [Reading(Guid.NewGuid(), heartRate: 160)];
        var facade = new SafetyMonitoringContextFacade(AlertRepository(), Service());

        Assert.False(await facade.EvaluateSyncBatchAsync(2, Batch));
        Assert.True(await facade.EvaluateSyncBatchAsync(Agency, Batch));
        Assert.True(await facade.EvaluateSyncBatchAsync(Agency, Batch));
        Assert.Single(_savedAlerts);
    }

    private Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>> Evaluate(int agencyId = Agency) =>
        Service().Handle(new EvaluateSyncBatchCommand(agencyId, Batch), CancellationToken.None);

    private static IReadOnlyList<EarlyWarningAlert> Success(
        Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError> result) =>
        Assert.IsType<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSyncBatchError>.Success>(result).Value;

    private static TelemetryReadingSnapshot Reading(Guid clientRecordId, int? heartRate = 80, double? deviation = 20,
        int? member = Member) =>
        new(clientRecordId, member, 11, CapturedAt, CapturedAt.AddMinutes(30), -9.5, -77.5, 4200, heartRate, 90, deviation);

    // Each call builds a new service, like a new request or worker scope.
    private EarlyWarningAlertCommandService Service() => new(
        AlertRepository(),
        new ExternalTrackingSessionService(FieldTracking()),
        new ExternalRouteSetupService(ExpeditionSetup()),
        new ExternalCheckpointPassageService(Passages()),
        new ExternalSyncBatchService(SyncBatches()),
        UnitOfWork(),
        new FixedTimeProvider(Now),
        NullLogger<EarlyWarningAlertCommandService>.Instance);

    private ISyncBatchContextFacade SyncBatches() =>
        InterfaceStub<ISyncBatchContextFacade>.Create((method, args) =>
        {
            var found = (int)args[0]! == Agency && (int)args[1]! == Batch;
            switch (method.Name)
            {
                case nameof(ISyncBatchContextFacade.FindBatchForEvaluationAsync):
                    return Task.FromResult(found
                        ? new SyncBatchEvaluationInput(Batch, Agency, Session, 3, CapturedAt.AddMinutes(30), _readings, [],
                            _readings.Length, 0, 0, _evaluationStatus)
                        : null);
                case nameof(ISyncBatchContextFacade.StageEvaluationCompletedAsync):
                    _evaluationStaged = found;
                    return Task.FromResult(found);
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });

    private IFieldTrackingContextFacade FieldTracking() =>
        InterfaceStub<IFieldTrackingContextFacade>.Create((method, args) => method.Name switch
        {
            nameof(IFieldTrackingContextFacade.SessionExistsInAgencyAsync) =>
                Task.FromResult((int)args[0]! == Agency && (int)args[1]! == Session),
            nameof(IFieldTrackingContextFacade.IsSessionActiveAsync) =>
                Task.FromResult(_sessionActive && (int)args[0]! == Session),
            nameof(IFieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync) =>
                Task.FromResult<int?>((int)args[0]! == Session ? Group : null),
            _ => throw new InvalidOperationException(method.Name)
        });

    private static IExpeditionSetupContextFacade ExpeditionSetup() =>
        InterfaceStub<IExpeditionSetupContextFacade>.Create((method, args) => method.Name switch
        {
            nameof(IExpeditionSetupContextFacade.FindParticipantBaselinesByExpeditionGroup)
                when (int)args[0]! == Agency && (int)args[1]! == Group =>
                Task.FromResult<IReadOnlyList<ParticipantBaseline>>([new ParticipantBaseline(Member, 50, 140)]),
            nameof(IExpeditionSetupContextFacade.FindExpectedTimeWindowsByExpeditionGroup)
                when (int)args[0]! == Agency && (int)args[1]! == Group =>
                Task.FromResult<IReadOnlyList<(int FromCheckpointId, int ToCheckpointId, int MaximumMinutes)>>([(3, 4, 60)]),
            _ => throw new InvalidOperationException(method.Name)
        });

    // The group passed checkpoint 3 two hours before the capture and has not reached checkpoint 4.
    private static ICheckpointSyncContextFacade Passages() =>
        InterfaceStub<ICheckpointSyncContextFacade>.Create((method, args) =>
            method.Name == nameof(ICheckpointSyncContextFacade.GetLastPassageTimeAtCheckpointBySessionAsync)
                ? Task.FromResult<DateTimeOffset?>((int)args[1]! == 3 ? CapturedAt.AddHours(-2) : null)
                : throw new InvalidOperationException(method.Name));

    private IEarlyWarningAlertRepository AlertRepository() =>
        InterfaceStub<IEarlyWarningAlertRepository>.Create((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(IEarlyWarningAlertRepository.AddAsync):
                    _pendingAlerts.Add((EarlyWarningAlert)args[0]!);
                    return Task.CompletedTask;
                case nameof(IEarlyWarningAlertRepository.FindExistingEvidenceKeysAsync):
                    var requested = (IReadOnlyCollection<string>)args[1]!;
                    IReadOnlySet<string> existing = _savedAlerts
                        .Where(alert => alert.SessionId == (int)args[0]! && requested.Contains(alert.EvidenceKey))
                        .Select(alert => alert.EvidenceKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    return Task.FromResult(existing);
                case nameof(IEarlyWarningAlertRepository.FindBySessionIdAndEvidenceKeyAsync):
                    return Task.FromResult(_savedAlerts.FirstOrDefault(alert =>
                        alert.SessionId == (int)args[0]! && alert.EvidenceKey == (string)args[1]!));
                case nameof(IEarlyWarningAlertRepository.ExistsBySessionIdCheckpointIdAndTypeAsync):
                    return Task.FromResult(_savedAlerts.Any(alert => alert.SessionId == (int)args[0]! &&
                        alert.CheckpointId == (int)args[1]! && alert.AlertType == (AlertType)args[2]!));
                case nameof(IEarlyWarningAlertRepository.Update):
                    return null;
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });

    // Saving commits the staged alerts and the evaluated status together; a failure discards both, like a lost scope.
    private IUnitOfWork UnitOfWork() =>
        InterfaceStub<IUnitOfWork>.Create((method, _) =>
        {
            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            if (_saveFailure is not null)
            {
                _pendingAlerts.Clear();
                _evaluationStaged = false;
                return Task.FromException(_saveFailure);
            }
            _saves++;
            _savedAlerts.AddRange(_pendingAlerts);
            _pendingAlerts.Clear();
            if (_evaluationStaged) _evaluationStatus = "evaluated";
            _evaluationStaged = false;
            return Task.CompletedTask;
        });
}
