using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Application.Internal.QueryServices;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public class TrackingAuthorizationTests
{
    [Theory]
    [InlineData(1, 101, true)]
    [InlineData(1, 102, true)]
    [InlineData(1, 201, false)]
    [InlineData(2, 101, true)]
    [InlineData(2, 102, false)]
    [InlineData(2, 201, false)]
    [InlineData(4, 101, true)]
    [InlineData(4, 102, false)]
    [InlineData(4, 201, false)]
    public async Task FacadeEnforcesAgencyAssignedGuideAndTouristMembership(int userId, int groupId, bool allowed)
    {
        var data = new TrackingAccessData();
        Assert.Equal(allowed, await data.Expeditions.CanAccessExpeditionGroup(data.Actor(userId), groupId));
    }

    [Theory]
    [InlineData("field_guide", null, null)]
    [InlineData("field_guide", 0, null)]
    [InlineData("adventure_tourist", null, null)]
    [InlineData("adventure_tourist", null, " ")]
    [InlineData("unknown", null, null)]
    public async Task FacadeFailsClosedForUnlinkedOrUnknownActors(string role, int? guideId, string? document)
    {
        var data = new TrackingAccessData();
        var actor = new AuthenticatedActor(1, 1, role, guideId, document);
        Assert.False(await data.Expeditions.CanAccessExpeditionGroup(actor, 101));
        Assert.Empty(await data.Expeditions.FindAccessibleExpeditionGroupIds(actor));
        Assert.Null(data.LastScope);
    }

    [Fact]
    public async Task GuideListContainsOnlyAssignedGroups()
    {
        var data = new TrackingAccessData();
        var service = new TrackingSessionQueryService(data.SessionRepository, data.Expeditions);
        var sessions = await service.Handle(new GetAllSessionsByAgencyIdQuery(1), data.Actor(2), default);
        Assert.Equal(11, Assert.Single(sessions).Id);
        Assert.Equal(10, data.LastScope!.FieldGuideId);
    }

    [Fact]
    public async Task ForgedQueryAgencyDoesNotOverrideAuthenticatedAgency()
    {
        var data = new TrackingAccessData();
        var service = new TrackingSessionQueryService(data.SessionRepository, data.Expeditions);
        Assert.Empty(await service.Handle(new GetAllSessionsByAgencyIdQuery(2), data.Actor(1), default));
        Assert.Null(await service.Handle(new GetActiveSessionByGroupIdQuery(2, 201), data.Actor(1), default));
    }

    [Fact]
    public async Task TelemetryIsNotReadBeforeSessionOwnershipIsValidated()
    {
        var data = new TrackingAccessData();
        var service = new TelemetryRecordQueryService(data.TelemetryRepository, data.SessionRepository, data.Expeditions);
        Assert.Null(await service.Handle(new GetTelemetryRecordsBySessionIdQuery(1, 21), data.Actor(1), default));
        Assert.Null(await service.Handle(new GetLatestTelemetryRecordByWearableIdQuery(1, 12, 101), data.Actor(2), default));
        Assert.Null(await service.Handle(new GetTelemetryRecordsBySessionIdQuery(1, 11), data.Actor(4), default));
        Assert.Equal(0, data.TelemetryReads);
    }

    [Theory]
    [InlineData(1, 101, EStartTrackingSessionError.AccessForbidden)]
    [InlineData(4, 101, EStartTrackingSessionError.AccessForbidden)]
    [InlineData(2, 102, EStartTrackingSessionError.GroupNotFound)]
    [InlineData(2, 201, EStartTrackingSessionError.GroupNotFound)]
    public async Task UnauthorizedStartDoesNotPersist(int userId, int groupId, EStartTrackingSessionError expected)
    {
        var data = new TrackingAccessData();
        var service = new TrackingSessionCommandService(data.SessionRepository, data.Expeditions, data.UnitOfWork,
            NullLogger<TrackingSessionCommandService>.Instance);
        var result = await service.Handle(new StartTrackingSessionCommand(1, groupId), data.Actor(userId), default);
        Assert.Equal(expected, Assert.IsType<Result<TrackingSession, EStartTrackingSessionError>.Failure>(result).Error);
        Assert.Equal(0, data.Writes);
        Assert.Equal(0, data.Commits);
    }

    [Fact]
    public async Task UnassignedGuideCannotEndSessionOrSubmitTelemetry()
    {
        var data = new TrackingAccessData();
        var sessions = new TrackingSessionCommandService(data.SessionRepository, data.Expeditions, data.UnitOfWork,
            NullLogger<TrackingSessionCommandService>.Instance);
        var end = await sessions.Handle(new EndTrackingSessionCommand(1, 12), data.Actor(2), default);
        Assert.Equal(EEndTrackingSessionError.SessionNotFound,
            Assert.IsType<Result<TrackingSession, EEndTrackingSessionError>.Failure>(end).Error);
        var records = new TelemetryRecordCommandService(data.SessionRepository, data.TelemetryRepository,
            data.Expeditions, data.UnitOfWork, NullLogger<TelemetryRecordCommandService>.Instance);
        var submit = await records.Handle(new SubmitTelemetryBatchCommand(1, 12, 101, []), data.Actor(2), default);
        Assert.Equal(ESubmitTelemetryBatchError.SessionNotFound,
            Assert.IsType<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure>(submit).Error);
        Assert.True(data.Sessions.Single(session => session.Id == 12).IsActive);
        Assert.Equal(0, data.Writes);
        Assert.Equal(0, data.Commits);
    }
}
