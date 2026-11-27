using System.Collections.Concurrent;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.Rest.Transform;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.Support;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     Test data of the Incident Response workflow. Agency 1 has group 101 (session 11, guide A) and group 102 (session
///     12, guide B); agency 2 has group 201 (session 21). Alerts 501 and 503 belong to session 11, 502 to session 12.
/// </summary>
internal sealed class IncidentWorkflowData
{
    public const int Admin = 1, GuideA = 2, GuideB = 3, Tourist = 4, OtherAgencyAdmin = 5;
    public const string AllowedRecipient = "team-test@vitaltrek.test";

    public readonly User[] Users =
    [
        WithId(new User(new Username("admin@vitaltrek.test"), "test-hash", "Administrator", Role.OperationsAdministrator, 1), 1),
        WithId(new User(new Username("guide-a@vitaltrek.test"), "test-hash", "Guide A", Role.FieldGuide, 1, 10), 2),
        WithId(new User(new Username("guide-b@vitaltrek.test"), "test-hash", "Guide B", Role.FieldGuide, 1, 20), 3),
        WithId(new User(new Username("tourist@vitaltrek.test"), "test-hash", "Tourist", Role.AdventureTourist, 1,
            identityDocument: "70123456"), 4),
        WithId(new User(new Username("admin@other.test"), "test-hash", "Other agency", Role.OperationsAdministrator, 2), 5)
    ];

    // Session -> (agency, group, active).
    private readonly Dictionary<int, (int Agency, int Group, bool Active)> _sessions = new()
    {
        [11] = (1, 101, true), [12] = (1, 102, true), [13] = (1, 101, false), [21] = (2, 201, true)
    };

    private readonly AlertReference[] _alerts =
    [
        new(501, 11, 101, "active"), new(502, 12, 102, "active"), new(503, 11, 101, "dismissed"),
        new(504, 11, 101, "acknowledged")
    ];

    public InMemoryUnitOfWork UnitOfWork { get; } = new();
    public InMemoryIncidentRepository Incidents { get; }
    public InMemoryEvacuationActionRepository EvacuationActions { get; }
    public InMemoryNotificationRepository Notifications { get; }
    public FakeNotificationProvider Provider { get; } = new();

    public IncidentWorkflowData()
    {
        Incidents = new InMemoryIncidentRepository(UnitOfWork);
        EvacuationActions = new InMemoryEvacuationActionRepository(UnitOfWork);
        Notifications = new InMemoryNotificationRepository(UnitOfWork);
    }

    public AuthenticatedActor Actor(int userId) =>
        AuthenticatedActorFromEntityAssembler.ToActorFromEntity(Users.Single(user => user.Id == userId));

    public IReadOnlyCollection<int> GroupsOf(AuthenticatedActor actor) => actor switch
    {
        { AgencyId: 1, RoleCode: "operations_administrator" } => [101, 102],
        { AgencyId: 2, RoleCode: "operations_administrator" } => [201],
        { AgencyId: 1, FieldGuideId: 10 } => [101],
        { AgencyId: 1, FieldGuideId: 20 } => [102],
        { AgencyId: 1, RoleCode: "adventure_tourist" } => [101],
        _ => []
    };

    public IExpeditionSetupContextFacade ExpeditionSetup() =>
        InterfaceStub<IExpeditionSetupContextFacade>.Create((method, args) => method.Name switch
        {
            nameof(IExpeditionSetupContextFacade.CanAccessExpeditionGroup) =>
                Task.FromResult(GroupsOf((AuthenticatedActor)args[0]!).Contains((int)args[1]!)),
            nameof(IExpeditionSetupContextFacade.FindAccessibleExpeditionGroupIds) =>
                Task.FromResult(GroupsOf((AuthenticatedActor)args[0]!)),
            _ => throw new InvalidOperationException(method.Name)
        });

    public IFieldTrackingContextFacade FieldTracking() =>
        InterfaceStub<IFieldTrackingContextFacade>.Create((method, args) => method.Name switch
        {
            nameof(IFieldTrackingContextFacade.SessionExistsInAgencyAsync) => Task.FromResult(
                _sessions.TryGetValue((int)args[1]!, out var session) && session.Agency == (int)args[0]!),
            nameof(IFieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync) => Task.FromResult<int?>(
                _sessions.TryGetValue((int)args[0]!, out var session) ? session.Group : null),
            nameof(IFieldTrackingContextFacade.IsSessionActiveAsync) => Task.FromResult(
                _sessions.TryGetValue((int)args[0]!, out var session) && session.Active),
            _ => throw new InvalidOperationException(method.Name)
        });

    public ISafetyMonitoringContextFacade SafetyMonitoring() =>
        InterfaceStub<ISafetyMonitoringContextFacade>.Create((method, args) => method.Name switch
        {
            nameof(ISafetyMonitoringContextFacade.FindAlertReferenceAsync) => Task.FromResult(
                (int)args[0]! == 1 ? _alerts.FirstOrDefault(alert => alert.AlertId == (int)args[1]!) : null),
            _ => throw new InvalidOperationException(method.Name)
        });

    public IIamContextFacade Iam() =>
        InterfaceStub<IIamContextFacade>.Create((method, args) => method.Name switch
        {
            // The team test address is registered as a staff account of agency 1; tourists are never recipients.
            nameof(IIamContextFacade.IsAgencyStaffEmail) => Task.FromResult((int)args[0]! == 1 &&
                (string.Equals((string)args[1]!, AllowedRecipient, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals((string)args[1]!, "guide-a@vitaltrek.test", StringComparison.OrdinalIgnoreCase))),
            _ => throw new InvalidOperationException(method.Name)
        });

    public static T WithId<T>(T entity, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

/// <summary>Unit of work that assigns identifiers to added aggregates on commit, like the database does.</summary>
internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _pending = [];
    public int Commits { get; private set; }

    public void Enlist(Action commit) => _pending.Add(commit);

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var commit in _pending) commit();
        _pending.Clear();
        Commits++;
        return Task.CompletedTask;
    }

    public void DiscardChanges() => _pending.Clear();
}

internal abstract class InMemoryRepository<T>(InMemoryUnitOfWork unitOfWork) : IBaseRepository<T> where T : class
{
    private int _nextId = 1;
    protected readonly List<T> Items = [];

    public int Count => Items.Count;

    public IReadOnlyList<T> All => Items;

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        unitOfWork.Enlist(() =>
        {
            IncidentWorkflowData.WithId(entity, _nextId++);
            Items.Add(entity);
        });
        return Task.CompletedTask;
    }

    public Task<T?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(item => (int)typeof(T).GetProperty("Id")!.GetValue(item)! == id));

    public void Update(T entity)
    {
    }

    public void Remove(T entity) => Items.Remove(entity);

    public Task<IEnumerable<T>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<T>>(Items.ToArray());
}

internal sealed class InMemoryIncidentRepository(InMemoryUnitOfWork unitOfWork)
    : InMemoryRepository<Incident>(unitOfWork), IIncidentRepository
{
    public Task<Incident?> FindByIdAndAgencyIdAsync(int incidentId, int agencyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(incident => incident.Id == incidentId && incident.AgencyId == agencyId));

    public Task<IEnumerable<Incident>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Incident>>(Items
            .Where(incident => incident.SessionId == sessionId && incident.AgencyId == agencyId).ToArray());

    public Task<IEnumerable<Incident>> FindOpenByAgencyIdAndGroupIdsAsync(int agencyId,
        IReadOnlyCollection<int> groupIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Incident>>(Items.Where(incident => incident.AgencyId == agencyId &&
            groupIds.Contains(incident.ExpeditionGroupId) && incident.IsOpen).ToArray());

    public Task<Incident?> FindByAlertIdAsync(int agencyId, int alertId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(incident => incident.AgencyId == agencyId && incident.AlertId == alertId));

    public Task<IReadOnlyList<Incident>> FindAllBySessionIdAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Incident>>(Items
            .Where(incident => incident.AgencyId == agencyId && incident.SessionId == sessionId).ToArray());

    public Task<int> CountBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(incident => incident.SessionId == sessionId));

    public Task<bool> ExistsOpenBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(incident => incident.SessionId == sessionId && incident.IsOpen));
}

internal sealed class InMemoryEvacuationActionRepository(InMemoryUnitOfWork unitOfWork)
    : InMemoryRepository<EvacuationAction>(unitOfWork), IEvacuationActionRepository
{
    public Task<EvacuationAction?> FindByClientActionIdAsync(int agencyId, Guid clientActionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(action => action.AgencyId == agencyId &&
                                                       action.ClientActionId == clientActionId));

    public Task<IEnumerable<EvacuationAction>> FindByIncidentIdAsync(int agencyId, int incidentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<EvacuationAction>>(Items
            .Where(action => action.AgencyId == agencyId && action.IncidentId == incidentId)
            .OrderBy(action => action.RecordedAt).ToArray());

    public Task<int> CountByIncidentIdsAsync(int agencyId, IReadOnlyCollection<int> incidentIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(action => action.AgencyId == agencyId && incidentIds.Contains(action.IncidentId)));
}

internal sealed class InMemoryNotificationRepository(InMemoryUnitOfWork unitOfWork)
    : InMemoryRepository<IncidentNotification>(unitOfWork), IIncidentNotificationRepository
{
    public Task<IEnumerable<IncidentNotification>> FindByIncidentIdAndAgencyIdAsync(int incidentId, int agencyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<IncidentNotification>>(Items
            .Where(notification => notification.IncidentId == incidentId && notification.AgencyId == agencyId)
            .ToArray());

    public Task<IncidentNotification?> FindByNotificationIdAsync(int agencyId, Guid notificationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(notification => notification.AgencyId == agencyId &&
                                                             notification.NotificationId == notificationId));
}

/// <summary>
///     Test double of an external e-mail provider. It keeps one message per idempotency key, like a provider that
///     honours idempotency keys, so a retry after a lost response returns the first message identifier.
/// </summary>
internal sealed class FakeNotificationProvider : IIncidentNotificationSender
{
    public enum Mode { Accept, Reject, NotConfigured, AcceptThenLoseResponse, RateLimited }

    private readonly ConcurrentDictionary<string, string> _messagesByKey = new();
    public Mode Behaviour { get; set; } = Mode.Accept;
    public IncidentNotificationDeliveryOutcome DeliveryOutcome { get; set; } = IncidentNotificationDeliveryOutcome.Accepted;
    public List<IncidentNotificationMessage> Calls { get; } = [];
    public int DistinctMessages => _messagesByKey.Count;

    public async Task<IncidentNotificationSendResult> SendAsync(IncidentNotificationMessage message,
        CancellationToken cancellationToken)
    {
        lock (Calls) Calls.Add(message);
        switch (Behaviour)
        {
            case Mode.Reject:
                return IncidentNotificationSendResult.Rejected("provider_validation_error");
            case Mode.NotConfigured:
                return IncidentNotificationSendResult.NotConfigured();
            case Mode.RateLimited:
                return IncidentNotificationSendResult.Uncertain("provider_rate_limited");
            case Mode.AcceptThenLoseResponse:
                // The provider stores the message, but the answer never reaches the platform before the timeout.
                _messagesByKey.GetOrAdd(message.IdempotencyKey, _ => $"msg_{Guid.NewGuid():N}");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                throw new InvalidOperationException("The response should have been lost.");
            default:
                return IncidentNotificationSendResult.Accepted(
                    _messagesByKey.GetOrAdd(message.IdempotencyKey, _ => $"msg_{Guid.NewGuid():N}"));
        }
    }

    public Task<IncidentNotificationDeliveryStatus> GetDeliveryStatusAsync(string providerMessageId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new IncidentNotificationDeliveryStatus(DeliveryOutcome));
}
