using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.Rest.Transform;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.Support;

namespace VitalTrek.API.Tests.FieldTracking;

/// <summary>
///     Isolated persistence boundaries for access tests. The real application services and Expedition Setup facade
///     run against these fixtures; neither the developer's database nor production is contacted.
/// </summary>
internal sealed class TrackingAccessData
{
    public readonly User[] Users =
    [
        WithId(new User(new Username("admin@example.test"), "test-hash", "Administrator",
            Role.OperationsAdministrator, 1), 1),
        WithId(new User(new Username("guide@example.test"), "test-hash", "Assigned guide",
            Role.FieldGuide, 1, fieldGuideId: 10), 2),
        WithId(new User(new Username("guide2@example.test"), "test-hash", "Other guide",
            Role.FieldGuide, 1, fieldGuideId: 11), 3),
        WithId(new User(new Username("tourist@example.test"), "test-hash", "Tourist",
            Role.AdventureTourist, 1, identityDocument: "DOC-A"), 4),
        WithId(new User(new Username("other@example.test"), "test-hash", "Other agency",
            Role.OperationsAdministrator, 2), 5)
    ];

    public readonly List<TrackingSession> Sessions =
    [
        WithId(new TrackingSession(new StartTrackingSessionCommand(1, 101)), 11),
        WithId(new TrackingSession(new StartTrackingSessionCommand(1, 102)), 12),
        WithId(new TrackingSession(new StartTrackingSessionCommand(2, 201)), 21)
    ];

    private readonly ExpeditionGroup[] _groups = [Group(101, 1, 10), Group(102, 1, 11), Group(201, 2, 20)];
    private readonly TelemetryRecord[] _records =
    [
        WithId(new TelemetryRecord(11, 100, new TelemetryRecordEntry(-9.5, -77.5, 3000,
            DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid(), 80, 90, 0)), 1001),
        WithId(new TelemetryRecord(12, 101, new TelemetryRecordEntry(-9.4, -77.4, 3100,
            DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid(), 80, 90, 0)), 1002),
        WithId(new TelemetryRecord(21, 200, new TelemetryRecordEntry(-9.3, -77.3, 3200,
            DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid(), 80, 90, 0)), 2001)
    ];

    public int TelemetryReads { get; private set; }
    public int Writes { get; private set; }
    public int Commits { get; private set; }
    public AccessScope? LastScope { get; private set; }
    public IExpeditionSetupContextFacade Expeditions { get; }
    public ITrackingSessionRepository SessionRepository { get; }
    public ITelemetryRecordRepository TelemetryRepository { get; }
    public IUnitOfWork UnitOfWork { get; }

    public TrackingAccessData()
    {
        var groups = InterfaceStub<IExpeditionGroupRepository>.Create((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(IExpeditionGroupRepository.FindByScopeAsync):
                    LastScope = (AccessScope)args[0]!;
                    return Task.FromResult(VisibleGroups(LastScope));
                case nameof(IExpeditionGroupRepository.FindByIdAndScopeAsync):
                    LastScope = (AccessScope)args[1]!;
                    return Task.FromResult(VisibleGroups(LastScope).FirstOrDefault(group => group.Id == (int)args[0]!));
                default: throw new InvalidOperationException(method.Name);
            }
        });
        Expeditions = new ExpeditionSetupContextFacade(InterfaceStub<IFieldGuideRepository>.Create(),
            InterfaceStub<IManifestEntryRepository>.Create(), groups, InterfaceStub<ICheckpointRepository>.Create(),
            InterfaceStub<IExpectedTimeWindowRepository>.Create(), InterfaceStub<IExpeditionStartService>.Create(),
            InterfaceStub<IParticipantPreparationRepository>.Create(), InterfaceStub<IWearableDeviceRepository>.Create());
        SessionRepository = InterfaceStub<ITrackingSessionRepository>.Create((method, args) => method.Name switch
        {
            nameof(ITrackingSessionRepository.FindAllByAgencyIdAsync) => Task.FromResult<IEnumerable<TrackingSession>>(
                Sessions.Where(session => session.AgencyId == (int)args[0]!).ToArray()),
            nameof(ITrackingSessionRepository.FindByIdAndAgencyIdAsync) => Task.FromResult(Sessions.FirstOrDefault(
                session => session.Id == (int)args[0]! && session.AgencyId == (int)args[1]!)),
            nameof(ITrackingSessionRepository.FindActiveByGroupIdAsync) => Task.FromResult(Sessions.FirstOrDefault(
                session => session.ExpeditionGroupId == (int)args[0]! && session.IsActive)),
            nameof(ITrackingSessionRepository.ExistsActiveByGroupIdAsync) => Task.FromResult(Sessions.Any(
                session => session.ExpeditionGroupId == (int)args[0]! && session.IsActive)),
            nameof(ITrackingSessionRepository.AddAsync) => RecordWrite(),
            nameof(ITrackingSessionRepository.Update) => RecordUpdate(),
            _ => throw new InvalidOperationException(method.Name)
        });
        TelemetryRepository = InterfaceStub<ITelemetryRecordRepository>.Create((method, args) =>
        {
            if (method.Name == nameof(ITelemetryRecordRepository.AddRangeAsync)) return RecordWrite();
            TelemetryReads++;
            return method.Name switch
            {
                nameof(ITelemetryRecordRepository.FindAllBySessionIdAsync) => Task.FromResult<IEnumerable<TelemetryRecord>>(
                    _records.Where(record => record.SessionId == (int)args[0]!).ToArray()),
                nameof(ITelemetryRecordRepository.FindLatestBySessionIdAndWearableIdAsync) => Task.FromResult(
                    _records.FirstOrDefault(record => record.SessionId == (int)args[0]! &&
                        record.WearableDeviceId == (int)args[1]!)),
                _ => throw new InvalidOperationException(method.Name)
            };
        });
        UnitOfWork = InterfaceStub<IUnitOfWork>.Create((method, _) =>
        {
            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            Commits++;
            return Task.CompletedTask;
        });
    }

    public AuthenticatedActor Actor(int userId) =>
        AuthenticatedActorFromEntityAssembler.ToActorFromEntity(Users.Single(user => user.Id == userId));

    private IEnumerable<ExpeditionGroup> VisibleGroups(AccessScope scope) =>
        _groups.Where(group => group.AgencyId == scope.AgencyId &&
            (scope.FieldGuideId is null || group.FieldGuideId == scope.FieldGuideId) &&
            (scope.IdentityDocument is null || scope.IdentityDocument == "DOC-A" && group.Id == 101));

    private Task RecordWrite() { Writes++; return Task.CompletedTask; }
    private object? RecordUpdate() { Writes++; return null; }

    private static ExpeditionGroup Group(int id, int agencyId, int guideId)
    {
        var today = new DateOnly(2026, 11, 25);
        var group = new ExpeditionGroup(new CreateExpeditionGroupCommand(agencyId, 1, $"Group {id}", today, 10), today);
        group.AssignFieldGuide(guideId);
        return WithId(group, id);
    }

    private static T WithId<T>(T entity, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}
