using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Application.ACL;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Internal.QueryServices;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TrackingProgressMySqlTests
{
    [LocalMySqlFact]
    public async Task SqlProjectionFiltersMembersAndKeepsLatestCaptureDespiteDelayedReception()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var context = database.Open();
        var now = DateTimeOffset.UtcNow;
        (await context.Set<ExpeditionGroup>().SingleAsync()).Start(now.AddHours(-1));
        var session = new TrackingSession(new StartTrackingSessionCommand(1, 101), now.AddHours(-1));
        context.Add(session);
        await new UnitOfWork(context).CompleteAsync();
        context.Add(Capture(session.Id, 60, 70, 80, now.AddMinutes(-2), now.AddMinutes(-1)));
        context.Add(Capture(session.Id, 60, 70, 111, now.AddMinutes(-20), now.AddSeconds(-30)));
        context.Add(Capture(session.Id, 61, 71, 199, now.AddMinutes(-3), now.AddMinutes(-1)));
        context.Add(new CheckpointPassage(new RecordCheckpointPassageCommand(1, 10, session.Id, 30,
            now.AddMinutes(-20)), now.AddMinutes(-19)));
        await new UnitOfWork(context).CompleteAsync();
        context.ChangeTracker.Clear();

        var sessions = new TrackingSessionRepository(context);
        var records = new TelemetryRecordRepository(context);
        var expedition = new ExpeditionSetupContextFacade(new FieldGuideRepository(context),
            new ManifestEntryRepository(context), new ExpeditionGroupRepository(context), new CheckpointRepository(context),
            new ExpectedTimeWindowRepository(context), InterfaceStub<IExpeditionStartService>.Create(),
            new ParticipantPreparationRepository(context), new WearableDeviceRepository(context));
        var tracking = new FieldTrackingContextFacade(sessions, expedition, InterfaceStub<ITelemetryRecordCommandService>.Create());
        var service = new TrackingProgressQueryService(sessions, records, expedition,
            new CheckpointSyncContextFacade(new CheckpointPassageRepository(context), tracking));
        var tourist = new AuthenticatedActor(4, 1, "adventure_tourist", IdentityDocument: "DOC-0");
        var own = await service.Handle(new GetTrackingProgressBySessionIdQuery(1, session.Id), tourist, default);
        Assert.NotNull(own);
        Assert.Equal(80, Assert.Single(own.Participants).HeartRate);
        Assert.Equal(60, own.Participants[0].MemberId);
        Assert.Equal(50, own.EstimatedProgressPercentage);
        Assert.Equal(30, own.LastConfirmedCheckpoint!.CheckpointId);
        var guide = new AuthenticatedActor(2, 1, "field_guide", 10);
        var all = await service.Handle(new GetTrackingProgressByGroupIdQuery(1, 101), guide, default);
        Assert.NotNull(all);
        Assert.Equal(2, all.Participants.Count);
        Assert.Equal(own.SynchronizedAt, all.SynchronizedAt);
        var again = await service.Handle(new GetTrackingProgressByGroupIdQuery(1, 101), guide, default);
        Assert.Equal(all.SynchronizedAt, again!.SynchronizedAt);
        Assert.Null(await service.Handle(new GetTrackingProgressBySessionIdQuery(1, session.Id),
            new AuthenticatedActor(3, 1, "field_guide", 11), default));
        Assert.Null(await service.Handle(new GetTrackingProgressBySessionIdQuery(2, session.Id),
            new AuthenticatedActor(5, 2, "operations_administrator"), default));
        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static TelemetryRecord Capture(int sessionId, int memberId, int wearableId, int heartRate,
        DateTimeOffset recordedAt, DateTimeOffset synchronizedAt) => new(sessionId, wearableId,
        new TelemetryRecordEntry(-9.5, -77.5, 3000, recordedAt, Guid.NewGuid(), heartRate, 90, 0), memberId, synchronizedAt);
}
