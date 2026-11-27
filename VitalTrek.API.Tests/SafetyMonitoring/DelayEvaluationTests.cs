using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
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
///     D02 and D T3: delays are evaluated only for the segment that leaves the furthest confirmed checkpoint, measured
///     from its captured passage. A repeated evaluation never duplicates a delay, a worse delay escalates the same open
///     alert and a closed alert is never reopened.
/// </summary>
public sealed class DelayEvaluationTests
{
    private const int Agency = 1, Session = 7, Group = 5, AssignedGuide = 10, OtherGuide = 11;
    private static readonly DateTimeOffset Departure = new(2026, 11, 26, 10, 0, 0, TimeSpan.FromHours(-5));

    private readonly FixedTimeProvider _clock = new(Departure);
    private readonly Dictionary<int, DateTimeOffset> _passedAt = new();
    private readonly List<EarlyWarningAlert> _savedAlerts = [];
    private readonly List<EarlyWarningAlert> _pendingAlerts = [];
    private bool _sessionActive = true;
    private int _saves;

    [Fact]
    public async Task OverdueSegmentWithoutArrivalRaisesADelayMeasuredFromTheCapturedPassage()
    {
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(61);

        var alert = Assert.Single(Success(await Evaluate()));

        Assert.Equal(SafetyRule.CheckpointDelay, alert.Rule);
        Assert.Equal(AlertType.Delay, alert.AlertType);
        Assert.Equal(2, alert.CheckpointId);
        Assert.Null(alert.MemberId);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal("checkpoint_delay:2", alert.EvidenceKey);
        Assert.Equal(new AlertEvidence(61, null, 60, AlertEvidence.Minutes, Departure), alert.Evidence);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task ArrivalCapturedOnTimeAndReceivedLaterProducesNoDelayForTheCompletedSegment()
    {
        _passedAt[1] = Departure;
        // The arrival was captured on time; the batch that confirms it arrives much later.
        _passedAt[2] = Departure.AddMinutes(50);
        _clock.UtcNow = Departure.AddMinutes(100);

        Assert.Empty(Success(await Evaluate()));
        Assert.Empty(_savedAlerts);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task OnlyTheSegmentAfterTheFurthestConfirmedCheckpointIsEvaluated()
    {
        _passedAt[1] = Departure;
        _passedAt[3] = Departure.AddMinutes(100);
        _clock.UtcNow = Departure.AddMinutes(170);

        var alert = Assert.Single(Success(await Evaluate()));

        Assert.Equal(4, alert.CheckpointId);
        Assert.Equal(70m, alert.Evidence!.ObservedValue);
        Assert.DoesNotContain(_savedAlerts, saved => saved.CheckpointId == 2);
    }

    [Fact]
    public async Task RepeatedEvaluationDoesNotDuplicateTheDelay()
    {
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(61);
        Success(await Evaluate());

        _clock.UtcNow = Departure.AddMinutes(75);
        Assert.Empty(Success(await Evaluate()));

        Assert.Single(_savedAlerts);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task WorseDelayEscalatesTheSameOpenAlert()
    {
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(61);
        var first = Assert.Single(Success(await Evaluate()));

        _clock.UtcNow = Departure.AddMinutes(125);
        var escalated = Assert.Single(Success(await Evaluate()));

        Assert.Same(first, escalated);
        Assert.Single(_savedAlerts);
        Assert.Equal(AlertSeverity.Critical, escalated.Severity);
        Assert.Equal(125m, escalated.Evidence!.ObservedValue);
        Assert.Equal(2, _saves);
    }

    [Fact]
    public async Task ClosedDelayIsNeverReopenedByALaterEvaluation()
    {
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(61);
        var alert = Assert.Single(Success(await Evaluate()));
        alert.Dismiss(9, _clock.UtcNow);

        _clock.UtcNow = Departure.AddMinutes(125);
        Assert.Empty(Success(await Evaluate()));

        Assert.Equal(AlertStatus.Dismissed, alert.Status);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task DelayAlertRaisedBeforeEvidenceIdentitiesPreventsADuplicate()
    {
        _savedAlerts.Add(new EarlyWarningAlert(new RaiseEarlyWarningAlertCommand(Agency, Session, 2, AlertType.Delay,
            AlertSeverity.High, "Delay reported before this version."), Group, Departure.AddMinutes(65)));
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(90);

        Assert.Empty(Success(await Evaluate()));
        Assert.Single(_savedAlerts);
    }

    [Fact]
    public async Task InactiveSessionIsNotEvaluated()
    {
        _sessionActive = false;
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(90);

        Assert.Equal(EEvaluateSessionDelaysError.SessionNotActive, Failure(await Evaluate()));
        Assert.Empty(_savedAlerts);
    }

    [Fact]
    public async Task UnassignedGuideOrAnotherAgencyCannotRequestTheEvaluation()
    {
        _passedAt[1] = Departure;
        _clock.UtcNow = Departure.AddMinutes(90);
        var command = new EvaluateSessionDelaysCommand(Agency, Session);

        var unassigned = await Service().Handle(command, Guide(OtherGuide), CancellationToken.None);
        var otherAgency = await Service().Handle(command,
            new AuthenticatedActor(20, 2, "operations_administrator"), CancellationToken.None);

        Assert.Equal(EEvaluateSessionDelaysError.SessionNotFound, Failure(unassigned));
        Assert.Equal(EEvaluateSessionDelaysError.SessionNotFound, Failure(otherAgency));
        Assert.Empty(_savedAlerts);

        Assert.Single(Success(await Service().Handle(command, Guide(AssignedGuide), CancellationToken.None)));
    }

    private static AuthenticatedActor Guide(int fieldGuideId) => new(30 + fieldGuideId, Agency, "field_guide", fieldGuideId);

    private Task<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>> Evaluate() =>
        Service().Handle(new EvaluateSessionDelaysCommand(Agency, Session), CancellationToken.None);

    private static IReadOnlyList<EarlyWarningAlert> Success(
        Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError> result) =>
        Assert.IsType<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>.Success>(result).Value;

    private static EEvaluateSessionDelaysError Failure(
        Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError> result) =>
        Assert.IsType<Result<IReadOnlyList<EarlyWarningAlert>, EEvaluateSessionDelaysError>.Failure>(result).Error;

    private EarlyWarningAlertCommandService Service() => new(
        AlertRepository(),
        new ExternalTrackingSessionService(InterfaceStub<IFieldTrackingContextFacade>.Create((method, args) =>
            method.Name switch
            {
                nameof(IFieldTrackingContextFacade.SessionExistsInAgencyAsync) =>
                    Task.FromResult((int)args[0]! == Agency && (int)args[1]! == Session),
                nameof(IFieldTrackingContextFacade.IsSessionActiveAsync) =>
                    Task.FromResult(_sessionActive && (int)args[0]! == Session),
                nameof(IFieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync) =>
                    Task.FromResult<int?>((int)args[0]! == Session ? Group : null),
                _ => throw new InvalidOperationException(method.Name)
            })),
        new ExternalRouteSetupService(InterfaceStub<IExpeditionSetupContextFacade>.Create((method, args) =>
            method.Name switch
            {
                // Route 1 -> 2 -> 3 -> 4 with a 60 minute maximum per segment, listed out of order.
                nameof(IExpeditionSetupContextFacade.FindExpectedTimeWindowsByExpeditionGroup)
                    when (int)args[0]! == Agency && (int)args[1]! == Group =>
                    Task.FromResult<IReadOnlyList<(int FromCheckpointId, int ToCheckpointId, int MaximumMinutes)>>(
                        [(3, 4, 60), (1, 2, 60), (2, 3, 60)]),
                nameof(IExpeditionSetupContextFacade.CanAccessExpeditionGroup) => Task.FromResult(
                    args[0] is AuthenticatedActor { AgencyId: Agency } actor && (int)args[1]! == Group &&
                    (actor.RoleCode == "operations_administrator" || actor.FieldGuideId == AssignedGuide)),
                _ => throw new InvalidOperationException(method.Name)
            })),
        new ExternalCheckpointPassageService(InterfaceStub<ICheckpointSyncContextFacade>.Create((method, args) =>
            method.Name == nameof(ICheckpointSyncContextFacade.GetLastPassageTimeAtCheckpointBySessionAsync) &&
            (int)args[0]! == Session
                ? Task.FromResult<DateTimeOffset?>(_passedAt.TryGetValue((int)args[1]!, out var passedAt) ? passedAt : null)
                : throw new InvalidOperationException(method.Name))),
        new ExternalSyncBatchService(InterfaceStub<ISyncBatchContextFacade>.Create()),
        InterfaceStub<IUnitOfWork>.Create((method, _) =>
        {
            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            _saves++;
            _savedAlerts.AddRange(_pendingAlerts);
            _pendingAlerts.Clear();
            return Task.CompletedTask;
        }),
        _clock,
        NullLogger<EarlyWarningAlertCommandService>.Instance);

    private IEarlyWarningAlertRepository AlertRepository() =>
        InterfaceStub<IEarlyWarningAlertRepository>.Create((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(IEarlyWarningAlertRepository.AddAsync):
                    _pendingAlerts.Add((EarlyWarningAlert)args[0]!);
                    return Task.CompletedTask;
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
}
