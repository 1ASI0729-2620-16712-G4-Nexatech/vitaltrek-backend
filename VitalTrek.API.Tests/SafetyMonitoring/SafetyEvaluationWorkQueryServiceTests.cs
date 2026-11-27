using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Internal.QueryServices;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring;

/// <summary>The pending work of the periodic evaluation is read through the Checkpoint Sync and Field Tracking facades.</summary>
public class SafetyEvaluationWorkQueryServiceTests
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 11, 26, 15, 0, 0, TimeSpan.Zero);
    private readonly List<string> _lookups = [];

    [Fact]
    public async Task ListsThePendingBatchesAndTheActiveSessionsOfEveryAgency()
    {
        var service = Service();

        var batches = await service.Handle(new GetPendingSyncBatchEvaluationsQuery(20), CancellationToken.None);
        var sessions = await service.Handle(new GetActiveTrackingSessionsQuery(30), CancellationToken.None);

        Assert.Equal(new[] { new PendingSyncBatch(50, 1), new PendingSyncBatch(51, 2) }, batches);
        Assert.Equal(new[] { new ActiveSession(7, 1) }, sessions);
        Assert.Equal(new[] { "batches:20", "sessions:30" }, _lookups);
    }

    [Fact]
    public async Task NonPositiveLimitReturnsNothingWithoutQuerying()
    {
        var service = Service();

        Assert.Empty(await service.Handle(new GetPendingSyncBatchEvaluationsQuery(0), CancellationToken.None));
        Assert.Empty(await service.Handle(new GetActiveTrackingSessionsQuery(-1), CancellationToken.None));
        Assert.Empty(_lookups);
    }

    private SafetyEvaluationWorkQueryService Service() => new(
        new ExternalSyncBatchService(InterfaceStub<ISyncBatchContextFacade>.Create((method, args) =>
        {
            if (method.Name != nameof(ISyncBatchContextFacade.FindPendingEvaluationsAsync))
                throw new InvalidOperationException(method.Name);
            _lookups.Add($"batches:{args[0]}");
            return Task.FromResult<IReadOnlyList<PendingSyncBatchEvaluation>>(
                [new PendingSyncBatchEvaluation(50, 1, 7, ReceivedAt), new PendingSyncBatchEvaluation(51, 2, 8, ReceivedAt)]);
        })),
        new ExternalTrackingSessionService(InterfaceStub<IFieldTrackingContextFacade>.Create((method, args) =>
        {
            if (method.Name != nameof(IFieldTrackingContextFacade.FindActiveSessionsAsync))
                throw new InvalidOperationException(method.Name);
            _lookups.Add($"sessions:{args[0]}");
            return Task.FromResult<IReadOnlyList<ActiveTrackingSession>>([new ActiveTrackingSession(7, 1, 5)]);
        })));
}
