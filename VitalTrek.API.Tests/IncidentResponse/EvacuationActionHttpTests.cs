using Xunit;
using static VitalTrek.API.Tests.IncidentResponse.IncidentWorkflowData;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     I T3 through the real HTTP pipeline: an evacuation action captured offline is sent with its stable client
///     identity; the retries after a reload persist a single action and keep the author and the original capture time.
/// </summary>
public sealed class EvacuationActionHttpTests(IncidentApiFixture fixture) : IClassFixture<IncidentApiFixture>
{
    [Fact]
    public async Task OfflineActionSentTwiceAfterAReloadPersistsOnceWithItsAuthorAndCaptureTime()
    {
        var incidentId = await fixture.ReportEmergency();
        var capturedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
        // The queued action survives the reload as JSON; the retries send exactly the stored payload.
        var queued = new
        {
            clientActionId = Guid.NewGuid(), description = "Group moved to the shelter at checkpoint 3.",
            location = "Checkpoint 3", status = "in_progress", recordedAt = capturedAt
        };
        var path = $"/api/v1/incidents/{incidentId}/evacuation-actions";

        var (first, created) = await fixture.Send("POST", path, GuideA, queued);
        var (second, replay) = await fixture.Send("POST", path, GuideA, queued);
        var (third, _) = await fixture.Send("POST", path, GuideA, queued);

        Assert.Equal(201, first);
        Assert.Equal(200, second);
        Assert.Equal(200, third);
        Assert.Equal(created.GetProperty("id").GetInt32(), replay.GetProperty("id").GetInt32());
        var stored = Assert.Single(fixture.Data.EvacuationActions.All,
            action => action.ClientActionId == queued.clientActionId);
        Assert.Equal(GuideA, stored.RecordedByUserId);
        Assert.Equal(capturedAt.UtcTicks - capturedAt.UtcTicks % 10, stored.RecordedAt.UtcTicks - stored.RecordedAt.UtcTicks % 10);
        Assert.True(stored.SynchronizedAt >= stored.RecordedAt);

        var (listed, list) = await fixture.Send("GET", path, Admin);
        Assert.Equal(200, listed);
        Assert.Single(list.EnumerateArray(),
            item => item.GetProperty("clientActionId").GetGuid() == queued.clientActionId);
    }

    [Fact]
    public async Task ReusedClientIdentityWithDifferentContentIsRejectedWithoutChangingTheFirstRecord()
    {
        var incidentId = await fixture.ReportEmergency();
        var action = new
        {
            clientActionId = Guid.NewGuid(), description = "Descent started.", location = (string?)null,
            status = "in_progress", recordedAt = DateTimeOffset.UtcNow.AddSeconds(-20)
        };
        var path = $"/api/v1/incidents/{incidentId}/evacuation-actions";
        Assert.Equal(201, (await fixture.Send("POST", path, GuideA, action)).Status);

        var (status, body) = await fixture.Send("POST", path, GuideA, action with { status = "completed" });

        Assert.Equal(409, status);
        Assert.Equal("EvacuationAction.IdentityConflict", body.GetProperty("code").GetString());
        var stored = Assert.Single(fixture.Data.EvacuationActions.All,
            item => item.ClientActionId == action.clientActionId);
        Assert.Equal("in_progress", VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects
            .EvacuationActionStatusExtensions.ToCode(stored.Status));
    }

    [Fact]
    public async Task ActionsRequireADeclaredEmergencyAndTheAssignedGuide()
    {
        var confirmedOnly = await fixture.ReportIncident();
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{confirmedOnly}/confirm", GuideA)).Status);
        var action = new
        {
            clientActionId = Guid.NewGuid(), description = "Evacuation step.", location = "Camp",
            status = "in_progress", recordedAt = DateTimeOffset.UtcNow
        };
        var (status, body) = await fixture.Send("POST", $"/api/v1/incidents/{confirmedOnly}/evacuation-actions",
            GuideA, action);
        Assert.Equal(409, status);
        Assert.Equal("EvacuationAction.EmergencyNotDeclared", body.GetProperty("code").GetString());

        var emergency = await fixture.ReportEmergency();
        var path = $"/api/v1/incidents/{emergency}/evacuation-actions";
        Assert.Equal(403, (await fixture.Send("POST", path, Admin, action)).Status);
        Assert.Equal(403, (await fixture.Send("POST", path, Tourist, action)).Status);
        Assert.Equal(404, (await fixture.Send("POST", path, GuideB, action)).Status);
        Assert.Equal(404, (await fixture.Send("GET", path, GuideB)).Status);
        Assert.DoesNotContain(fixture.Data.EvacuationActions.All, item => item.ClientActionId == action.clientActionId);
    }

    [Fact]
    public async Task CaptureMadeDuringTheEmergencyIsAcceptedWhenItArrivesAfterTheResolution()
    {
        var incidentId = await fixture.ReportEmergency();
        var capturedDuringEmergency = DateTimeOffset.UtcNow;
        await Task.Delay(50);
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{incidentId}/resolve", GuideA)).Status);
        var path = $"/api/v1/incidents/{incidentId}/evacuation-actions";

        var (late, _) = await fixture.Send("POST", path, GuideA, new
        {
            clientActionId = Guid.NewGuid(), description = "Participant handed to the ambulance.", location = "Trailhead",
            status = "completed", recordedAt = capturedDuringEmergency
        });
        var (future, _) = await fixture.Send("POST", path, GuideA, new
        {
            clientActionId = Guid.NewGuid(), description = "Capture time in the future.", location = "Base",
            status = "completed", recordedAt = DateTimeOffset.UtcNow.AddMinutes(5)
        });

        Assert.Equal(201, late);
        Assert.Equal(400, future);
    }
}
