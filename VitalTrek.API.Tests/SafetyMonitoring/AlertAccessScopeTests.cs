using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Internal.QueryServices;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring;

/// <summary>
///     D03: alert reads are limited to the groups the actor can see. An administrator sees the agency, a field guide
///     their assigned groups, and an adventure tourist the delays of their group and the alerts about themselves, never
///     the health evidence of another participant.
/// </summary>
public sealed class AlertAccessScopeTests
{
    private const int Agency = 1, GroupA = 5, GroupB = 6, SessionA = 7, SessionB = 8;
    private const int TouristMember = 60, OtherMember = 61, GroupBMember = 70, GuideA = 3, GuideB = 4;
    private static readonly DateTimeOffset CapturedAt = new(2026, 11, 26, 9, 30, 0, TimeSpan.FromHours(-5));

    private static readonly AuthenticatedActor Admin = new(1, Agency, "operations_administrator");
    private static readonly AuthenticatedActor GuideOfA = new(2, Agency, "field_guide", GuideA);
    private static readonly AuthenticatedActor GuideOfB = new(3, Agency, "field_guide", GuideB);
    private static readonly AuthenticatedActor Tourist = new(4, Agency, "adventure_tourist", IdentityDocument: "70123456");

    private readonly EarlyWarningAlert _delay = WithId(101, Delay(SessionA, GroupA));
    private readonly EarlyWarningAlert _touristHeartRate = WithId(102, HeartRate(SessionA, GroupA, TouristMember));
    private readonly EarlyWarningAlert _otherHeartRate = WithId(103, HeartRate(SessionA, GroupA, OtherMember));
    private readonly EarlyWarningAlert _manual = WithId(104, new EarlyWarningAlert(new RaiseEarlyWarningAlertCommand(Agency,
        SessionA, null, AlertType.Other, AlertSeverity.Medium, "Participant 61 reports dizziness."), GroupA, CapturedAt));
    private readonly EarlyWarningAlert _groupBHeartRate = WithId(105, HeartRate(SessionB, GroupB, GroupBMember));

    private EarlyWarningAlert[] Alerts => [_delay, _touristHeartRate, _otherHeartRate, _manual, _groupBHeartRate];

    [Fact]
    public async Task AdministratorSeesTheOpenAlertsOfEveryGroupOfTheAgency()
    {
        var alerts = await Service().Handle(new GetActiveAlertsByAgencyIdQuery(Agency), Admin, CancellationToken.None);

        Assert.Equal(Alerts, alerts);
    }

    [Fact]
    public async Task FieldGuideSeesOnlyTheAlertsOfTheirAssignedGroups()
    {
        var alerts = await Service().Handle(new GetActiveAlertsByAgencyIdQuery(Agency), GuideOfA, CancellationToken.None);

        Assert.Equal(new[] { _delay, _touristHeartRate, _otherHeartRate, _manual }, alerts);
    }

    [Fact]
    public async Task GuideOfAnotherGroupCannotReadTheAlertOrTheSession()
    {
        var service = Service();

        Assert.Null(await service.Handle(new GetAlertByIdQuery(Agency, 102), GuideOfB, CancellationToken.None));
        Assert.Empty(await service.Handle(new GetAlertsBySessionIdQuery(Agency, SessionA), GuideOfB, CancellationToken.None));
        Assert.Same(_groupBHeartRate,
            await service.Handle(new GetAlertByIdQuery(Agency, 105), GuideOfB, CancellationToken.None));
    }

    [Fact]
    public async Task TouristSeesTheGroupDelayAndOnlyTheAlertsAboutThemselves()
    {
        var service = Service();

        var active = await service.Handle(new GetActiveAlertsByAgencyIdQuery(Agency), Tourist, CancellationToken.None);
        var session = await service.Handle(new GetAlertsBySessionIdQuery(Agency, SessionA), Tourist, CancellationToken.None);

        Assert.Equal(new[] { _delay, _touristHeartRate }, active);
        Assert.Equal(new[] { _delay, _touristHeartRate }, session);
        Assert.Same(_touristHeartRate, await service.Handle(new GetAlertByIdQuery(Agency, 102), Tourist, CancellationToken.None));
        Assert.Null(await service.Handle(new GetAlertByIdQuery(Agency, 103), Tourist, CancellationToken.None));
        Assert.Null(await service.Handle(new GetAlertByIdQuery(Agency, 104), Tourist, CancellationToken.None));
    }

    [Fact]
    public async Task ActorOfAnotherAgencySeesNothing()
    {
        var otherAgency = new AuthenticatedActor(9, 2, "operations_administrator");
        var service = Service();

        Assert.Empty(await service.Handle(new GetActiveAlertsByAgencyIdQuery(2), otherAgency, CancellationToken.None));
        Assert.Null(await service.Handle(new GetAlertByIdQuery(2, 101), otherAgency, CancellationToken.None));
        Assert.Empty(await service.Handle(new GetAlertsBySessionIdQuery(2, SessionA), otherAgency, CancellationToken.None));
    }

    private static EarlyWarningAlert Delay(int sessionId, int groupId) =>
        new(new RecordEvaluatedAlertCommand(Agency, sessionId, 2, null, SafetyRule.CheckpointDelay, AlertSeverity.High,
            new AlertEvidence(70, null, 60, AlertEvidence.Minutes, CapturedAt), "2", "Delayed by 70 minutes."),
            groupId, CapturedAt.AddMinutes(70));

    private static EarlyWarningAlert HeartRate(int sessionId, int groupId, int memberId)
    {
        var readingId = Guid.NewGuid();
        return new EarlyWarningAlert(new RecordEvaluatedAlertCommand(Agency, sessionId, null, memberId,
            SafetyRule.HeartRateOutsideBaseline, AlertSeverity.High,
            new AlertEvidence(160, 50, 140, AlertEvidence.BeatsPerMinute, CapturedAt, readingId), readingId.ToString(),
            "Heart rate outside the demonstration baseline."), groupId, CapturedAt.AddMinutes(30));
    }

    // The identifier is generated by the database; tests assign it the way Entity Framework Core does.
    private static EarlyWarningAlert WithId(int id, EarlyWarningAlert alert)
    {
        typeof(EarlyWarningAlert).GetProperty(nameof(EarlyWarningAlert.Id))!.SetValue(alert, id);
        return alert;
    }

    private static IReadOnlyCollection<int> GroupsOf(AuthenticatedActor actor) => actor switch
    {
        { AgencyId: not Agency } => [],
        { RoleCode: "operations_administrator" } => [GroupA, GroupB],
        { FieldGuideId: GuideA } => [GroupA],
        { FieldGuideId: GuideB } => [GroupB],
        { RoleCode: "adventure_tourist" } => [GroupA],
        _ => []
    };

    private EarlyWarningAlertQueryService Service() => new(
        InterfaceStub<IEarlyWarningAlertRepository>.Create((method, args) => method.Name switch
        {
            nameof(IEarlyWarningAlertRepository.FindByIdAndAgencyIdAsync) => Task.FromResult(
                Alerts.FirstOrDefault(alert => alert.Id == (int)args[0]! && alert.AgencyId == (int)args[1]!)),
            nameof(IEarlyWarningAlertRepository.FindOpenByAgencyIdAndGroupIdsAsync) =>
                Task.FromResult<IEnumerable<EarlyWarningAlert>>(Alerts.Where(alert => alert.AgencyId == (int)args[0]! &&
                    ((IReadOnlyCollection<int>)args[1]!).Contains(alert.ExpeditionGroupId) && alert.IsOpen).ToArray()),
            nameof(IEarlyWarningAlertRepository.FindBySessionIdAndAgencyIdAsync) =>
                Task.FromResult<IEnumerable<EarlyWarningAlert>>(Alerts.Where(alert =>
                    alert.SessionId == (int)args[0]! && alert.AgencyId == (int)args[1]!).ToArray()),
            _ => throw new InvalidOperationException(method.Name)
        }),
        new ExternalRouteSetupService(InterfaceStub<IExpeditionSetupContextFacade>.Create((method, args) =>
        {
            var actor = (AuthenticatedActor)args[0]!;
            return method.Name switch
            {
                nameof(IExpeditionSetupContextFacade.CanAccessExpeditionGroup) =>
                    Task.FromResult(GroupsOf(actor).Contains((int)args[1]!)),
                nameof(IExpeditionSetupContextFacade.FindAccessibleExpeditionGroupIds) =>
                    Task.FromResult(GroupsOf(actor)),
                // Expedition Setup resolves only the tourist's own manifest entries.
                nameof(IExpeditionSetupContextFacade.FindTrackingProgressContext) =>
                    Task.FromResult<TrackingProgressContext?>(GroupsOf(actor).Contains((int)args[1]!)
                        ? new TrackingProgressContext(1, GuideA, [],
                            actor.RoleCode == "adventure_tourist" ? [TouristMember] : [TouristMember, OtherMember])
                        : null),
                _ => throw new InvalidOperationException(method.Name)
            };
        })));
}
