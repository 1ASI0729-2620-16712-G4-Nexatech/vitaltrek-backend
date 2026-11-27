using VitalTrek.API.IncidentResponse.Application.ACL;
using Xunit;
using static VitalTrek.API.Tests.IncidentResponse.IncidentWorkflowData;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     Hand-off to Tour Closure: the facade computes the open incidents, emergencies and evacuation actions of a session
///     from persistence, scoped to the agency.
/// </summary>
public sealed class IncidentResponseFacadeTests(IncidentApiFixture fixture) : IClassFixture<IncidentApiFixture>
{
    [Fact]
    public async Task OverviewCountsOpenIncidentsEmergenciesAndActionsOfTheSessionOfTheAgency()
    {
        var emergency = await fixture.ReportEmergency();
        Assert.Equal(201, (await fixture.Send("POST", $"/api/v1/incidents/{emergency}/evacuation-actions", GuideA, new
        {
            clientActionId = Guid.NewGuid(), description = "Group moved to the shelter.", location = "Shelter",
            status = "completed", recordedAt = DateTimeOffset.UtcNow
        })).Status);
        var dismissed = await fixture.ReportIncident();
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{dismissed}/dismiss", GuideA)).Status);
        var reported = await fixture.ReportIncident();
        var facade = new IncidentResponseContextFacade(fixture.Data.Incidents, fixture.Data.EvacuationActions);

        var overview = await facade.GetSessionIncidentOverviewAsync(1, 11);
        var otherAgency = await facade.GetSessionIncidentOverviewAsync(2, 11);

        Assert.True(overview.TotalIncidents >= 3);
        Assert.Contains(emergency, overview.OpenIncidentIds);
        Assert.Contains(reported, overview.OpenIncidentIds);
        Assert.DoesNotContain(dismissed, overview.OpenIncidentIds);
        Assert.True(overview.EmergencyIncidents >= 1);
        Assert.True(overview.DismissedIncidents >= 1);
        Assert.True(overview.EvacuationActions >= 1);
        Assert.True(await facade.HasOpenIncidentsForSessionAsync(11));
        Assert.Equal(0, otherAgency.TotalIncidents);
        Assert.Empty(otherAgency.OpenIncidentIds);
    }
}
