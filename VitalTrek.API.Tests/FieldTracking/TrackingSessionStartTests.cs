using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TrackingSessionStartTests
{
    [Fact]
    public async Task StartActivatesGroupAndCreatesSessionWithTheSameTimeAndOneSave()
    {
        var data = new TrackingStartData();
        var result = await Start(data);
        var success = Assert.IsType<Result<TrackingSessionStartResult, EStartTrackingSessionError>.Success>(result).Value;
        Assert.True(success.WasCreated);
        Assert.True(success.Session.IsActive);
        Assert.Equal(ExpeditionGroupStatus.Active, data.Group.Status);
        Assert.Equal(data.Group.StartedAt, success.Session.StartedAt);
        Assert.Equal(1, data.Saves);
    }

    [Fact]
    public async Task RetryReturnsOriginalSessionAndTimeWithoutSavingOrRevalidatingPreparation()
    {
        var data = new TrackingStartData();
        await Start(data);
        var original = data.Session!;
        var startedAt = original.StartedAt;
        data.Preparations[0].RevokeConsent(DateTimeOffset.UtcNow);
        var retry = Assert.IsType<Result<TrackingSessionStartResult, EStartTrackingSessionError>.Success>(await Start(data)).Value;
        Assert.False(retry.WasCreated);
        Assert.Same(original, retry.Session);
        Assert.Equal(startedAt, retry.Session.StartedAt);
        Assert.Equal(startedAt, data.Group.StartedAt);
        Assert.Equal(1, data.Saves);
    }

    [Theory]
    [InlineData("draft-route", EStartTrackingSessionError.RouteNotReady)]
    [InlineData("one-checkpoint", EStartTrackingSessionError.RouteNotReady)]
    [InlineData("missing-segment", EStartTrackingSessionError.RouteNotReady)]
    [InlineData("reverse-segment", EStartTrackingSessionError.RouteNotReady)]
    [InlineData("missing-protocol", EStartTrackingSessionError.ProtocolNotReady)]
    [InlineData("inactive-protocol", EStartTrackingSessionError.ProtocolNotReady)]
    [InlineData("empty-manifest", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("capacity", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("missing-preparation", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("revoked-consent", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("inactive-wearable", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("missing-contact", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("missing-device", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("foreign-device", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("maintenance", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("duplicate-device", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("returned-device", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("device-used-elsewhere", EStartTrackingSessionError.ParticipantsNotReady)]
    [InlineData("missing-guide", EStartTrackingSessionError.GroupNotFound)]
    public async Task IncompletePreparationCannotActivateGroupOrCreateSession(string fault, EStartTrackingSessionError error)
    {
        var data = new TrackingStartData();
        ApplyFault(data, fault);
        var failure = Assert.IsType<Result<TrackingSessionStartResult, EStartTrackingSessionError>.Failure>(await Start(data));
        Assert.Equal(error, failure.Error);
        Assert.True(data.Group.IsScheduled);
        Assert.Null(data.Group.StartedAt);
        Assert.Null(data.Session);
        Assert.Equal(0, data.Saves);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviouslyStartedOrFinishedGroupCannotCreateANewSession(bool finished)
    {
        var data = new TrackingStartData();
        data.Group.Start(DateTimeOffset.UtcNow.AddHours(-2));
        if (finished) data.Group.Finish(DateTimeOffset.UtcNow.AddHours(-1));
        var failure = Assert.IsType<Result<TrackingSessionStartResult, EStartTrackingSessionError>.Failure>(await Start(data));
        Assert.Equal(EStartTrackingSessionError.GroupNotScheduled, failure.Error);
        Assert.Null(data.Session);
        Assert.Equal(0, data.Saves);
    }

    [Fact]
    public async Task ClosureFacadeStagesCompletionWithoutSavingAndPreservesTheFirstEndTime()
    {
        var data = new TrackingStartData();
        await Start(data);
        var closure = new FieldTrackingContextFacade(data.Sessions, data.Expeditions,
            VitalTrek.API.Tests.Support.InterfaceStub<VitalTrek.API.FieldTracking.Application.Services.ITelemetryRecordCommandService>.Create());
        var endedAt = data.Session!.StartedAt.AddHours(1);
        Assert.True(await closure.StageSessionEndAsync(data.Actor, data.Session.Id, endedAt));
        Assert.False(data.Session.IsActive);
        Assert.True(await closure.StageSessionEndAsync(data.Actor, data.Session.Id, endedAt.AddHours(1)));
        Assert.Equal(endedAt, data.Session.EndedAt);
        Assert.Equal(1, data.Saves);
    }

    internal static Task<Result<TrackingSessionStartResult, EStartTrackingSessionError>> Start(TrackingStartData data) =>
        new TrackingSessionCommandService(data.Sessions, data.Expeditions, data.UnitOfWork,
            NullLogger<TrackingSessionCommandService>.Instance)
            .Handle(new StartTrackingSessionCommand(1, 101), data.Actor, default);

    internal static void ApplyFault(TrackingStartData data, string fault)
    {
        switch (fault)
        {
            case "draft-route": TrackingStartData.Set(data.Route, "Status", RouteStatus.Draft); break;
            case "one-checkpoint": data.Checkpoints.RemoveAt(1); break;
            case "missing-segment": data.Windows.Clear(); break;
            case "reverse-segment": data.Windows[0] = new ExpectedTimeWindow(new CreateExpectedTimeWindowCommand(1, 20, 31, 30, 10, 60)); break;
            case "missing-protocol": data.Protocols.Clear(); break;
            case "inactive-protocol": data.Protocols[0].Update(new UpdateSafetyProtocolCommand(1, 50, "Protocol", SafetyProtocolStatus.Inactive)); break;
            case "empty-manifest": data.Members.Clear(); break;
            case "capacity": TrackingStartData.Set(data.Group, "MaximumCapacity", 1); break;
            case "missing-preparation": data.Preparations.RemoveAt(1); break;
            case "revoked-consent": data.Preparations[1].RevokeConsent(DateTimeOffset.UtcNow); break;
            case "inactive-wearable": TrackingStartData.Set(data.Preparations[1], "Activated", false); break;
            case "missing-contact": TrackingStartData.Set(data.Preparations[1], "EmergencyContactPhone", null); break;
            case "missing-device": data.Devices.RemoveAt(1); break;
            case "foreign-device": TrackingStartData.Set(data.Devices[1], "AgencyId", 2); break;
            case "maintenance": data.Devices[1].Release(WearableDeviceStatus.Maintenance); break;
            case "duplicate-device": data.Preparations[1].AssignWearable(70); data.Preparations[1].GrantConsent(DateTimeOffset.UtcNow); break;
            case "returned-device": data.Preparations[1].RegisterReturn(WearableDeviceStatus.Available, DateTimeOffset.UtcNow); break;
            case "device-used-elsewhere": data.WearableInUseElsewhere = true; break;
            case "missing-guide": data.GuideExists = false; break;
            default: throw new ArgumentOutOfRangeException(nameof(fault));
        }
    }
}
