using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using Xunit;

namespace VitalTrek.API.Tests.FieldTracking;

public sealed class TelemetryIngestionTests
{
    [Fact]
    public async Task InternalPortStagesWithoutSavingAndResolvesTheParticipantOnTheServer()
    {
        var data = await TelemetryIngestionData.CreateAsync();
        var entry = data.Reading();
        var facade = new FieldTrackingContextFacade(data.Ready.Sessions, data.Ready.Expeditions, data.Service());
        var input = new TelemetryReadingInput(entry.ClientRecordId, 70, entry.RecordedAt, entry.Latitude,
            entry.Longitude, entry.Altitude, entry.HeartRate, entry.Battery, entry.DeviationMeters);
        var result = await facade.StageTelemetryReadingAsync(data.Ready.Actor, 501, input);
        Assert.Equal(TelemetryReadingStageStatus.Accepted, result.Status);
        Assert.Equal(60, Assert.Single(data.Pending).MemberId);
        Assert.Empty(data.Stored);
        Assert.Equal(0, data.Saves);
        var retry = await facade.StageTelemetryReadingAsync(data.Ready.Actor, 501, input);
        Assert.Equal(TelemetryReadingStageStatus.Duplicate, retry.Status);
        Assert.Single(data.Pending);
        Assert.Equal(0, data.Saves);
    }

    [Fact]
    public async Task IdenticalRetryPreservesIdentityCaptureAndReceptionTimes()
    {
        var data = await TelemetryIngestionData.CreateAsync();
        var entry = data.Reading() with { RecordedAt = data.Reading().RecordedAt.ToOffset(TimeSpan.FromHours(-5)) };
        var command = new SubmitTelemetryBatchCommand(1, 501, 70, [entry]);
        var first = Assert.IsType<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Success>(
            await data.Service().Handle(command, data.Ready.Actor, default));
        var original = Assert.Single(first.Value);
        var receivedAt = original.SynchronizedAt;
        var retry = Assert.IsType<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Success>(
            await data.Service().Handle(command, data.Ready.Actor, default));
        Assert.Same(original, Assert.Single(retry.Value));
        Assert.Equal(receivedAt, original.SynchronizedAt);
        Assert.Equal(TimeSpan.Zero, original.RecordedAt.Offset);
        Assert.True(original.RecordedAt < receivedAt.AddMinutes(-9));
        Assert.Single(data.Stored);
        Assert.Equal(1, data.Saves);
    }

    [Fact]
    public async Task ReusingIdentityWithDifferentContentReturnsConflictWithoutOverwriting()
    {
        var data = await TelemetryIngestionData.CreateAsync();
        var entry = data.Reading();
        await data.Service().Handle(new SubmitTelemetryBatchCommand(1, 501, 70, [entry]), data.Ready.Actor, default);
        var result = await data.Service().Handle(new SubmitTelemetryBatchCommand(1, 501, 70,
            [entry with { HeartRate = 99 }]), data.Ready.Actor, default);
        Assert.Equal(ESubmitTelemetryBatchError.RecordIdentityConflict,
            Assert.IsType<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure>(result).Error);
        Assert.Equal(88, Assert.Single(data.Stored).HeartRate);
        Assert.Equal(1, data.Saves);
    }

    [Theory]
    [InlineData("consent", ESubmitTelemetryBatchError.ConsentRequired)]
    [InlineData("activation", ESubmitTelemetryBatchError.ConsentRequired)]
    [InlineData("returned", ESubmitTelemetryBatchError.WearableNotAssigned)]
    [InlineData("foreign-device", ESubmitTelemetryBatchError.WearableNotAssigned)]
    [InlineData("unassigned", ESubmitTelemetryBatchError.WearableNotAssigned)]
    [InlineData("closed", ESubmitTelemetryBatchError.SessionNotActive)]
    public async Task UnauthorizedPreparationCannotStageOrSave(string fault, ESubmitTelemetryBatchError expected)
    {
        var data = await TelemetryIngestionData.CreateAsync();
        switch (fault)
        {
            case "consent": data.Ready.Preparations[0].RevokeConsent(DateTimeOffset.UtcNow); break;
            case "activation": TrackingStartData.Set(data.Ready.Preparations[0], "Activated", false); break;
            case "returned": data.Ready.Preparations[0].RegisterReturn(WearableDeviceStatus.Available, DateTimeOffset.UtcNow); break;
            case "foreign-device": TrackingStartData.Set(data.Ready.Devices[0], "AgencyId", 2); break;
            case "unassigned": data.Ready.Preparations.Clear(); break;
            case "closed": data.Ready.Session!.End(); break;
        }
        var result = await data.Service().StageReading(new StageTelemetryReadingCommand(1, 501, 70, data.Reading()),
            data.Ready.Actor, default);
        Assert.Equal(expected, Assert.IsType<Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Failure>(result).Error);
        Assert.Empty(data.Pending);
        Assert.Empty(data.Stored);
        Assert.Equal(0, data.Saves);
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("heart-rate")]
    [InlineData("battery")]
    [InlineData("deviation")]
    [InlineData("latitude")]
    [InlineData("altitude")]
    [InlineData("future")]
    [InlineData("before-session")]
    public async Task InvalidCaptureRejectsTheWholeDirectRequestWithoutPartialSaves(string fault)
    {
        var data = await TelemetryIngestionData.CreateAsync();
        var good = data.Reading();
        var invalid = data.Reading();
        invalid = fault switch
        {
            "identity" => invalid with { ClientRecordId = Guid.Empty },
            "heart-rate" => invalid with { HeartRate = 0 },
            "battery" => invalid with { Battery = 101 },
            "deviation" => invalid with { DeviationMeters = -1 },
            "latitude" => invalid with { Latitude = double.NaN },
            "altitude" => invalid with { Altitude = double.PositiveInfinity },
            "future" => invalid with { RecordedAt = DateTimeOffset.UtcNow.AddHours(1) },
            "before-session" => invalid with { RecordedAt = DateTimeOffset.UtcNow.AddHours(-2) },
            _ => invalid
        };
        var result = await data.Service().Handle(new SubmitTelemetryBatchCommand(1, 501, 70, [good, invalid]),
            data.Ready.Actor, default);
        Assert.Equal(ESubmitTelemetryBatchError.InvalidData,
            Assert.IsType<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure>(result).Error);
        Assert.Empty(data.Pending);
        Assert.Empty(data.Stored);
        Assert.Equal(0, data.Saves);
    }
}
