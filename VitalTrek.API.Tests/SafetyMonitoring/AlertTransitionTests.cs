using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
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
///     D05 and D T5: every transition records its author and moment, a terminal alert cannot be reopened and an alert
///     of another agency is not found.
/// </summary>
public sealed class AlertTransitionTests
{
    private const int Agency = 1, AlertId = 30, Admin = 9, Guide = 10;
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);

    private readonly FixedTimeProvider _clock = new(Now);
    private readonly EarlyWarningAlert _alert = new(new RaiseEarlyWarningAlertCommand(Agency, 7, 3, AlertType.Other,
        AlertSeverity.Medium, "Reported by the guide."), 5, Now.AddMinutes(-30));
    private int _saves;

    [Fact]
    public async Task AcknowledgeThenResolveRecordsEachAuthorAndMoment()
    {
        Success(await Service().Handle(new AcknowledgeAlertCommand(Agency, AlertId, Guide), CancellationToken.None));
        _clock.UtcNow = Now.AddMinutes(25);
        var resolved = Success(await Service().Handle(new ResolveAlertCommand(Agency, AlertId, Admin), CancellationToken.None));

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
        Success(await Service().Handle(new DismissAlertCommand(Agency, AlertId, Guide), CancellationToken.None));

        var acknowledge = await Service().Handle(new AcknowledgeAlertCommand(Agency, AlertId, Admin), CancellationToken.None);
        var dismissAgain = await Service().Handle(new DismissAlertCommand(Agency, AlertId, Admin), CancellationToken.None);

        Assert.Equal(EUpdateAlertStatusError.InvalidTransition, Failure(acknowledge));
        Assert.Equal(EUpdateAlertStatusError.InvalidTransition, Failure(dismissAgain));
        Assert.Equal(AlertStatus.Dismissed, _alert.Status);
        Assert.Equal(Guide, _alert.ClosedByUserId);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public async Task AlertOfAnotherAgencyIsNotFoundAndNotChanged()
    {
        var result = await Service().Handle(new AcknowledgeAlertCommand(2, AlertId, Admin), CancellationToken.None);

        Assert.Equal(EUpdateAlertStatusError.AlertNotFound, Failure(result));
        Assert.Equal(AlertStatus.Active, _alert.Status);
        Assert.Null(_alert.AcknowledgedByUserId);
        Assert.Equal(0, _saves);
    }

    private static EarlyWarningAlert Success(Result<EarlyWarningAlert, EUpdateAlertStatusError> result) =>
        Assert.IsType<Result<EarlyWarningAlert, EUpdateAlertStatusError>.Success>(result).Value;

    private static EUpdateAlertStatusError Failure(Result<EarlyWarningAlert, EUpdateAlertStatusError> result) =>
        Assert.IsType<Result<EarlyWarningAlert, EUpdateAlertStatusError>.Failure>(result).Error;

    private EarlyWarningAlertCommandService Service() => new(
        InterfaceStub<IEarlyWarningAlertRepository>.Create((method, args) => method.Name switch
        {
            nameof(IEarlyWarningAlertRepository.FindByIdAndAgencyIdAsync) => Task.FromResult<EarlyWarningAlert?>(
                (int)args[0]! == AlertId && (int)args[1]! == Agency ? _alert : null),
            nameof(IEarlyWarningAlertRepository.Update) => null,
            _ => throw new InvalidOperationException(method.Name)
        }),
        new ExternalTrackingSessionService(InterfaceStub<IFieldTrackingContextFacade>.Create()),
        new ExternalRouteSetupService(InterfaceStub<IExpeditionSetupContextFacade>.Create()),
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
