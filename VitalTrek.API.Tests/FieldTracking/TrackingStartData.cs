using System.Reflection;
using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.Support;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.Tests.FieldTracking;

/// <summary>Real preparation aggregates with isolated repository boundaries for start and HTTP tests.</summary>
internal sealed class TrackingStartData
{
    public AuthenticatedActor Actor { get; } = new(2, 1, "field_guide", 10);
    public ExpeditionGroup Group { get; }
    public Route Route { get; }
    public FieldGuide Guide { get; } = WithId(new FieldGuide(new CreateFieldGuideCommand(1, "Guide")), 10);
    public List<Checkpoint> Checkpoints { get; } = [];
    public List<ExpectedTimeWindow> Windows { get; } = [];
    public List<SafetyProtocol> Protocols { get; } = [];
    public List<ManifestEntry> Members { get; } = [];
    public List<ParticipantPreparation> Preparations { get; } = [];
    public List<WearableDevice> Devices { get; } = [];
    public bool GuideExists { get; set; } = true;
    public bool WearableInUseElsewhere { get; set; }
    public TrackingSession? Session { get; private set; }
    public int Saves { get; private set; }
    public IExpeditionSetupContextFacade Expeditions { get; }
    public ITrackingSessionRepository Sessions { get; }
    public IUnitOfWork UnitOfWork { get; }

    public TrackingStartData()
    {
        var today = new DateOnly(2026, 11, 25);
        Group = WithId(new ExpeditionGroup(new CreateExpeditionGroupCommand(1, 20, "Ready group", today, 2), today), 101);
        Group.AssignFieldGuide(10);
        Route = WithId(new Route(new CreateRouteCommand(1, "Ready route", "Start", "Finish", 5, 500,
            RouteDifficulty.Beginner, 60)), 20);
        Route.Enable(2);
        Checkpoints.Add(WithId(new Checkpoint(new CreateCheckpointCommand(1, 20, "Start", 1, "BT-A", -9.5, -77.5)), 30));
        Checkpoints.Add(WithId(new Checkpoint(new CreateCheckpointCommand(1, 20, "Finish", 2, "BT-B", -9.4, -77.4)), 31));
        Windows.Add(WithId(new ExpectedTimeWindow(new CreateExpectedTimeWindowCommand(1, 20, 30, 31, 10, 60)), 40));
        Protocols.Add(WithId(new SafetyProtocol(new CreateSafetyProtocolCommand(1, 20, "Contact base in an emergency.")), 50));
        for (var index = 0; index < 2; index++)
        {
            Members.Add(WithId(new ManifestEntry(new CreateManifestEntryCommand(1, 101, $"Member {index}",
                $"DOC-{index}", 10)), 60 + index));
            var device = WithId(new WearableDevice(new CreateWearableDeviceCommand(1, $"DEV-{index}")), 70 + index);
            device.MarkAssigned();
            Devices.Add(device);
            var preparation = WithId(new ParticipantPreparation(1, 101, 60 + index), 80 + index);
            preparation.AssignWearable(device.Id);
            preparation.RegisterEmergencyContact(new EmergencyContact("Contact", "+51987654321", "Family"));
            preparation.GrantConsent(DateTimeOffset.UtcNow.AddMinutes(-5));
            Preparations.Add(preparation);
        }

        var groups = Stub<IExpeditionGroupRepository>((method, args) => method.Name switch
        {
            nameof(IExpeditionGroupRepository.FindByIdAndScopeAsync) => Task.FromResult(
                (int)args[0]! == Group.Id && Visible((AccessScope)args[1]!) ? Group : null),
            nameof(IExpeditionGroupRepository.FindByScopeAsync) => Task.FromResult<IEnumerable<ExpeditionGroup>>(
                Visible((AccessScope)args[0]!) ? [Group] : []),
            _ => throw new InvalidOperationException(method.Name)
        });
        var guides = Stub<IFieldGuideRepository>((_, args) => Task.FromResult(
            GuideExists && (int)args[0]! == Guide.Id && (int)args[1]! == Guide.AgencyId ? Guide : null));
        var routes = Stub<IRouteRepository>((_, args) => Task.FromResult(
            (int)args[0]! == Route.Id && (int)args[1]! == Route.AgencyId ? Route : null));
        var checkpoints = Stub<ICheckpointRepository>((_, _) => Task.FromResult<IEnumerable<Checkpoint>>(Checkpoints));
        var windows = Stub<IExpectedTimeWindowRepository>((_, _) => Task.FromResult<IEnumerable<ExpectedTimeWindow>>(Windows));
        var protocols = Stub<ISafetyProtocolRepository>((_, _) => Task.FromResult<IEnumerable<SafetyProtocol>>(Protocols));
        var manifest = Stub<IManifestEntryRepository>((_, _) => Task.FromResult<IEnumerable<ManifestEntry>>(Members));
        var preparations = Stub<IParticipantPreparationRepository>((method, _) => method.Name switch
        {
            nameof(IParticipantPreparationRepository.FindByScopeAsync) => Task.FromResult<IEnumerable<ParticipantPreparation>>(Preparations),
            nameof(IParticipantPreparationRepository.IsWearableInUseAsync) => Task.FromResult(WearableInUseElsewhere),
            _ => throw new InvalidOperationException(method.Name)
        });
        var devices = Stub<IWearableDeviceRepository>((_, args) => Task.FromResult(Devices.FirstOrDefault(device =>
            device.Id == (int)args[0]! && device.AgencyId == (int)args[1]!)));
        var start = new ExpeditionStartService(groups, guides, routes, checkpoints, windows, protocols, manifest,
            preparations, devices);
        Expeditions = new ExpeditionSetupContextFacade(guides, manifest, groups, checkpoints, windows, start);

        Sessions = Stub<ITrackingSessionRepository>((method, args) => method.Name switch
        {
            nameof(ITrackingSessionRepository.FindActiveByGroupIdAsync) => Task.FromResult(
                Session is { IsActive: true } && Session.ExpeditionGroupId == (int)args[0]! ? Session : null),
            nameof(ITrackingSessionRepository.FindByIdAndAgencyIdAsync) => Task.FromResult(
                Session?.Id == (int)args[0]! && Session.AgencyId == (int)args[1]! ? Session : null),
            nameof(ITrackingSessionRepository.AddAsync) => AddSession((TrackingSession)args[0]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        UnitOfWork = Stub<IUnitOfWork>((method, _) =>
        {
            if (method.Name == nameof(IUnitOfWork.DiscardChanges)) return null;
            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            Saves++;
            return Task.CompletedTask;
        });
    }

    private bool Visible(AccessScope scope) => scope.AgencyId == Group.AgencyId &&
        (scope.FieldGuideId is null || scope.FieldGuideId == Group.FieldGuideId);

    private Task AddSession(TrackingSession session)
    {
        Session = WithId(session, 501);
        return Task.CompletedTask;
    }

    private static T Stub<T>(Func<MethodInfo, object?[], object?> handler) where T : class => InterfaceStub<T>.Create(handler);

    internal static T WithId<T>(T entity, int id)
    {
        Set(entity, "Id", id);
        return entity;
    }

    internal static void Set<T>(T entity, string property, object? value) => typeof(T).GetProperty(property)!.SetValue(entity, value);
}
