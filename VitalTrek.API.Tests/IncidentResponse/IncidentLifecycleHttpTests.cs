using Xunit;
using static VitalTrek.API.Tests.IncidentResponse.IncidentWorkflowData;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     I T1 and I T2 through the real HTTP pipeline: the assigned guide verifies in the field, only the administrator
///     declares the emergency, other guides, tourists and agencies cannot change the incident, terminal states admit no
///     incompatible transition and an alert of another session cannot be linked.
/// </summary>
public sealed class IncidentLifecycleHttpTests(IncidentApiFixture fixture) : IClassFixture<IncidentApiFixture>
{
    [Fact]
    public async Task AssignedGuideConfirmsAndOnlyTheAdministratorDeclaresTheEmergency()
    {
        var id = await fixture.ReportIncident();

        var (confirmed, body) = await fixture.Send("PATCH", $"/api/v1/incidents/{id}/confirm", GuideA,
            new { note = "Verified in the field." });
        Assert.Equal(200, confirmed);
        Assert.Equal("confirmed", body.GetProperty("status").GetString());

        Assert.Equal(403, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/declare-emergency", GuideA)).Status);
        var (declared, emergency) = await fixture.Send("PATCH", $"/api/v1/incidents/{id}/declare-emergency", Admin);
        Assert.Equal(200, declared);
        Assert.Equal("emergency_declared", emergency.GetProperty("status").GetString());
        Assert.True(emergency.GetProperty("isOpen").GetBoolean());

        var changes = emergency.GetProperty("statusChanges").EnumerateArray().ToArray();
        Assert.Equal(["reported", "confirmed", "emergency_declared"],
            changes.Select(change => change.GetProperty("toStatus").GetString()));
        Assert.Equal(GuideA, changes[1].GetProperty("actorUserId").GetInt32());
        Assert.Equal("Verified in the field.", changes[1].GetProperty("note").GetString());
        Assert.Equal(Admin, changes[2].GetProperty("actorUserId").GetInt32());
    }

    [Fact]
    public async Task OtherGuideTouristAndOtherAgencyCannotChangeTheIncident()
    {
        var id = await fixture.ReportIncident();
        var commits = fixture.Data.UnitOfWork.Commits;

        Assert.Equal(404, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/confirm", GuideB)).Status);
        Assert.Equal(403, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/confirm", Tourist)).Status);
        Assert.Equal(404, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/declare-emergency",
            OtherAgencyAdmin)).Status);
        Assert.Equal(404, (await fixture.Send("GET", $"/api/v1/incidents/{id}", GuideB)).Status);
        Assert.Equal(404, (await fixture.Send("GET", $"/api/v1/incidents/{id}", OtherAgencyAdmin)).Status);
        Assert.Equal(403, (await fixture.Send("GET", $"/api/v1/incidents/{id}", Tourist)).Status);
        Assert.Equal(401, (await fixture.Send("GET", $"/api/v1/incidents/{id}")).Status);
        Assert.Equal(commits, fixture.Data.UnitOfWork.Commits);
        Assert.Equal(VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects.IncidentStatus.Reported,
            fixture.Data.Incidents.All.Single(incident => incident.Id == id).Status);
    }

    [Fact]
    public async Task AdministratorCannotConfirmBecauseFieldVerificationBelongsToTheGuide()
    {
        var id = await fixture.ReportIncident();
        Assert.Equal(403, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/confirm", Admin)).Status);
        Assert.Equal(403, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/dismiss", Admin)).Status);
    }

    [Fact]
    public async Task TouristCannotReportAndGuideCannotReportForAnotherGroup()
    {
        var report = new
        {
            sessionId = 11, incidentType = "medical", severity = "medium", description = "Dizziness.",
            reportedAt = DateTimeOffset.UtcNow
        };
        Assert.Equal(403, (await fixture.Send("POST", "/api/v1/incidents", Tourist, report)).Status);
        var (status, body) = await fixture.Send("POST", "/api/v1/incidents", GuideB, report);
        Assert.Equal(404, status);
        Assert.Equal("Incident.SessionNotFound", body.GetProperty("code").GetString());
        Assert.Equal(409, (await fixture.Send("POST", "/api/v1/incidents", GuideA, report with { sessionId = 13 }))
            .Status);
    }

    [Fact]
    public async Task DismissedAndClosedIncidentsRejectIncompatibleTransitions()
    {
        var dismissed = await fixture.ReportIncident();
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{dismissed}/dismiss", GuideA,
            new { note = "False alarm." })).Status);
        foreach (var transition in new[] { "confirm", "resolve", "close" })
        {
            var (status, body) = await fixture.Send("PATCH", $"/api/v1/incidents/{dismissed}/{transition}", GuideA);
            Assert.Equal(409, status);
            Assert.Equal("Incident.InvalidTransition", body.GetProperty("code").GetString());
        }

        var closed = await fixture.ReportIncident();
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{closed}/confirm", GuideA)).Status);
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{closed}/resolve", GuideA)).Status);
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{closed}/close", Admin)).Status);
        Assert.Equal(409, (await fixture.Send("PATCH", $"/api/v1/incidents/{closed}/resolve", GuideA)).Status);
        Assert.Equal(409, (await fixture.Send("PATCH", $"/api/v1/incidents/{closed}/declare-emergency", Admin)).Status);
        Assert.Equal(409, (await fixture.Send("PATCH", $"/api/v1/incidents/{closed}/status", GuideA,
            new { status = "dismissed" })).Status);
    }

    [Fact]
    public async Task ReportedIncidentCannotBeResolvedWithoutFieldVerification()
    {
        var id = await fixture.ReportIncident();
        Assert.Equal(409, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/resolve", GuideA)).Status);
    }

    [Fact]
    public async Task LegacyInResponseCodeConfirmsTheIncidentAndUnknownCodesAreRejected()
    {
        var id = await fixture.ReportIncident();
        Assert.Equal(400, (await fixture.Send("PATCH", $"/api/v1/incidents/{id}/status", GuideA,
            new { status = "teleported" })).Status);
        var (status, body) = await fixture.Send("PATCH", $"/api/v1/incidents/{id}/status", GuideA,
            new { status = "in_response" });
        Assert.Equal(200, status);
        Assert.Equal("confirmed", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AlertOfAnotherSessionIsRejectedAndEachAlertOriginatesOneIncident()
    {
        var report = new
        {
            sessionId = 11, incidentType = "medical", severity = "high", description = "Heart rate alert verified.",
            reportedAt = DateTimeOffset.UtcNow, alertId = (int?)502
        };
        var (outside, outsideBody) = await fixture.Send("POST", "/api/v1/incidents", GuideA, report);
        Assert.Equal(409, outside);
        Assert.Equal("Incident.AlertOutsideSession", outsideBody.GetProperty("code").GetString());

        var (missing, missingBody) = await fixture.Send("POST", "/api/v1/incidents", GuideA, report with { alertId = 999 });
        Assert.Equal(404, missing);
        Assert.Equal("Incident.AlertNotFound", missingBody.GetProperty("code").GetString());

        var (dismissed, dismissedBody) = await fixture.Send("POST", "/api/v1/incidents", GuideA,
            report with { alertId = 503 });
        Assert.Equal(409, dismissed);
        Assert.Equal("Incident.AlertDismissed", dismissedBody.GetProperty("code").GetString());

        var (created, createdBody) = await fixture.Send("POST", "/api/v1/incidents", GuideA, report with { alertId = 501 });
        Assert.Equal(201, created);
        Assert.Equal(501, createdBody.GetProperty("alertId").GetInt32());
        Assert.Equal(101, createdBody.GetProperty("expeditionGroupId").GetInt32());

        var (duplicate, duplicateBody) = await fixture.Send("POST", "/api/v1/incidents", Admin, report with { alertId = 501 });
        Assert.Equal(409, duplicate);
        Assert.Equal("Incident.AlertAlreadyLinked", duplicateBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ManualReportWithoutAlertRemainsAvailable()
    {
        var id = await fixture.ReportIncident();
        var (status, body) = await fixture.Send("GET", $"/api/v1/incidents/{id}", GuideA);
        Assert.Equal(200, status);
        Assert.Equal(JsonValueKindNull, body.GetProperty("alertId").ValueKind);
        Assert.Equal(GuideA, body.GetProperty("reportedByUserId").GetInt32());
    }

    [Fact]
    public async Task OpenIncidentListIsScopedToTheGroupsOfTheActor()
    {
        var groupA = await fixture.ReportIncident();
        var groupB = await fixture.ReportIncident(sessionId: 12, userId: GuideB);

        var (_, guideA) = await fixture.Send("GET", "/api/v1/incidents", GuideA);
        var (_, admin) = await fixture.Send("GET", "/api/v1/incidents", Admin);
        var (_, other) = await fixture.Send("GET", "/api/v1/incidents", OtherAgencyAdmin);
        var guideIds = guideA.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToArray();
        var adminIds = admin.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToArray();

        Assert.Contains(groupA, guideIds);
        Assert.DoesNotContain(groupB, guideIds);
        Assert.Contains(groupA, adminIds);
        Assert.Contains(groupB, adminIds);
        Assert.Empty(other.EnumerateArray());
        Assert.Equal(403, (await fixture.Send("GET", "/api/v1/incidents", Tourist)).Status);
    }

    private const System.Text.Json.JsonValueKind JsonValueKindNull = System.Text.Json.JsonValueKind.Null;
}
