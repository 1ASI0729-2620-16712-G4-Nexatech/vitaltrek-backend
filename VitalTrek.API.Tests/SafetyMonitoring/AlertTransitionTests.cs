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
///     D05, D03 and D T5: every transition records its author and moment, a terminal alert cannot be reopened, and only
///     an actor who can operate the group may raise or change its alerts; anything else is reported as not found.
/// </summary>
public sealed class AlertTransitionTests
{
    private const int Agency = 1, AlertId = 30, Session = 7, Group = 5, Admin = 9, Guide = 10, OtherGuide = 11;
    private const int AssignedGuideProfile = 3, OtherGuideProfile = 4;
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);
    private static readonly AuthenticatedActor AdminActor = new(Admin, Agency, "operations_administrator");
    private static readonly AuthenticatedActor GuideActor = new(Guide, Agency, "field_guide", AssignedGuideProfile);
    private static readonly AuthenticatedActor OtherGuideActor = new(OtherGuide, Agency, "field_guide", OtherGuideProfile);

    private readonly FixedTimeProvider _clock = new(Now);
    private readonly EarlyWarningAlert _alert = new(new RaiseEarlyWarningAlertCommand(Agency, Session, 3, AlertType.Other,
        AlertSeverity.Medium, "Reported by the guide."), Group, Now.AddMinutes(-30));
    private readonly List<EarlyWarningAlert> _added = [];
    private int _saves;

    [Fact]
    public async Task AcknowledgeThenResolveRecordsEachAuthorAndMoment()
    {
        Success(await Service().Handle(new AcknowledgeAlertCommand(Agency, AlertId, Guide), GuideActor,
            CancellationToken.None));
        _clock.UtcNow = Now.AddMinutes(25);
        var resolved = Success(await Service().Handle(new ResolveAlertCommand(Agency, AlertId, Admin), AdminActor,
            CancellationToken.None));

        Assert.Equal(AlertStatus.Resolved, resolved.Status);
        Assert.Equal(Guide, resolved.AcknowledgedByUserId);
        Assert.Equal(Now, resolved.AcknowledgedAt);
        Assert.Equal(Admin, resolved.ClosedByUserId);
        Assert.Equal(Now.AddMinutes(25), resolved.ClosedAt);
        Assert.Equal(2, _saves);
    }

    [Fact]
    public async Task TerminalAlertCannotBeReopenedAndKeepsItsCloser()
    {
        Success(await Service().Handle(new DismissAlertCommand(Agency, AlertId, Guide), GuideActor, CancellationToken.None));

        var acknowledge = await Service().Handle(new AcknowledgeAlertCommand(Agency, AlertId, Admin), AdminActor,
            CancellationToken.None);
        var dismissAgain = await Service().Handle(new DismissAlertCommand(Agency, AlertId, Admin), AdminActor,
            CancellationToken.None);

        Assert.Equal(EUpdateAlertStatusError.InvalidTransition, Failure(acknowledge));
        Assert.Equal(EUpdateAlertStatusError.InvalidTransition, Failure(dismissAgain));
        Assert.Equal(AlertStatus.Dismissed, _alert.Status);
        Assert.Equal(Guide, _alert.ClosedByUserId);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task AlertOfAnotherAgencyIsNotFoundAndNotChanged()
    {
        var otherAgencyAdmin = new AuthenticatedActor(20, 2, "operations_administrator");

        var result = await Service().Handle(new AcknowledgeAlertCommand(2, AlertId, 20), otherAgencyAdmin,
            CancellationToken.None);

        Assert.Equal(EUpdateAlertStatusError.AlertNotFound, Failure(result));
        Assert.Equal(AlertStatus.Active, _alert.Status);
        Assert.Null(_alert.AcknowledgedByUserId);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task GuideOfAnotherGroupCannotChangeTheAlert()
    {
        var result = await Service().Handle(new DismissAlertCommand(Agency, AlertId, OtherGuide), OtherGuideActor,
            CancellationToken.None);

        Assert.Equal(EUpdateAlertStatusError.AlertNotFound, Failure(result));
        Assert.Equal(AlertStatus.Active, _alert.Status);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task OnlyAnActorWhoCanOperateTheGroupRaisesAManualAlert()
    {
        var command = new RaiseEarlyWarningAlertCommand(Agency, Session, null, AlertType.Other, AlertSeverity.Medium,
            "Rockfall reported near the trail.");

        var rejected = await Service().Handle(command, OtherGuideActor, CancellationToken.None);
        var raised = await Service().Handle(command, GuideActor, CancellationToken.None);

        Assert.Equal(ERaiseEarlyWarningAlertError.SessionNotFound,
            Assert.IsType<Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>.Failure>(rejected).Error);
        var alert = Assert.IsType<Result<EarlyWarningAlert, ERaiseEarlyWarningAlertError>.Success>(raised).Value;
        Assert.Same(alert, Assert.Single(_added));
        Assert.Equal(Now, alert.RaisedAt);
        Assert.Equal(1, _saves);
    }

    private static EarlyWarningAlert Success(Result<EarlyWarningAlert, EUpdateAlertStatusError> result) =>
        Assert.IsType<Result<EarlyWarningAlert, EUpdateAlertStatusError>.Success>(result).Value;

    private static EUpdateAlertStatusError Failure(Result<EarlyWarningAlert, EUpdateAlertStatusError> result) =>
        Assert.IsType<Result<EarlyWarningAlert, EUpdateAlertStatusError>.Failure>(result).Error;

    private EarlyWarningAlertCommandService Service() => new(
        InterfaceStub<IEarlyWarningAlertRepository>.Create((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(IEarlyWarningAlertRepository.FindByIdAndAgencyIdAsync):
                    return Task.FromResult<EarlyWarningAlert?>(
                        (int)args[0]! == AlertId && (int)args[1]! == Agency ? _alert : null);
                case nameof(IEarlyWarningAlertRepository.AddAsync):
                    _added.Add((EarlyWarningAlert)args[0]!);
                    return Task.CompletedTask;
                case nameof(IEarlyWarningAlertRepository.Update):
                    return null;
                default:
                    throw new InvalidOperationException(method.Name);
            }
        }),
        new ExternalTrackingSessionService(InterfaceStub<IFieldTrackingContextFacade>.Create((method, args) =>
            method.Name switch
            {
                nameof(IFieldTrackingContextFacade.SessionExistsInAgencyAsync) =>
                    Task.FromResult((int)args[0]! == Agency && (int)args[1]! == Session),
                nameof(IFieldTrackingContextFacade.IsSessionActiveAsync) => Task.FromResult((int)args[0]! == Session),
                nameof(IFieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync) =>
                    Task.FromResult<int?>((int)args[0]! == Session ? Group : null),
                _ => throw new InvalidOperationException(method.Name)
            })),
        new ExternalRouteSetupService(InterfaceStub<IExpeditionSetupContextFacade>.Create((method, args) =>
            method.Name == nameof(IExpeditionSetupContextFacade.CanAccessExpeditionGroup)
                ? Task.FromResult(args[0] is AuthenticatedActor { AgencyId: Agency } actor && (int)args[1]! == Group &&
                                  (actor.RoleCode == "operations_administrator" ||
                                   actor.FieldGuideId == AssignedGuideProfile))
                : throw new InvalidOperationException(method.Name))),
        new ExternalCheckpointPassageService(InterfaceStub<ICheckpointSyncContextFacade>.Create()),
        new ExternalSyncBatchService(InterfaceStub<ISyncBatchContextFacade>.Create()),
        InterfaceStub<IUnitOfWork>.Create((method, _) =>
        {
            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            _saves++;
            return Task.CompletedTask;
        }),
        _clock,
        NullLogger<EarlyWarningAlertCommandService>.Instance);
}
