using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using Xunit;

namespace VitalTrek.API.Tests.IncidentResponse.Domain;

/// <summary>I01: explicit lifecycle transitions, terminal states, recorded history and legacy status codes.</summary>
public sealed class IncidentLifecycleTests
{
    private static readonly DateTimeOffset ReportedAt = new(2026, 11, 27, 9, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void FullLifecycleRecordsEveryTransitionWithItsActorAndMoment()
    {
        var incident = Reported();
        incident.Confirm(2, ReportedAt.AddMinutes(5), "Verified.");
        incident.DeclareEmergency(1, ReportedAt.AddMinutes(10));
        incident.Resolve(2, ReportedAt.AddMinutes(60));
        incident.Close(1, ReportedAt.AddMinutes(70));

        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.False(incident.IsOpen);
        Assert.Equal(ReportedAt.AddMinutes(5), incident.ConfirmedAt);
        Assert.Equal(ReportedAt.AddMinutes(10), incident.EmergencyDeclaredAt);
        Assert.Equal(ReportedAt.AddMinutes(60), incident.ResolvedAt);
        Assert.Equal(ReportedAt.AddMinutes(70), incident.ClosedAt);
        Assert.Equal([IncidentStatus.Reported, IncidentStatus.Confirmed, IncidentStatus.EmergencyDeclared,
            IncidentStatus.Resolved, IncidentStatus.Closed], incident.StatusChanges.Select(change => change.ToStatus));
        Assert.Equal([2, 2, 1, 2, 1], incident.StatusChanges.Select(change => change.ActorUserId));
        Assert.Null(incident.StatusChanges[0].FromStatus);
        Assert.Equal("Verified.", incident.StatusChanges[1].Note);
    }

    [Theory]
    [InlineData(IncidentStatus.Reported, IncidentStatus.Confirmed, true)]
    [InlineData(IncidentStatus.Reported, IncidentStatus.Dismissed, true)]
    [InlineData(IncidentStatus.Reported, IncidentStatus.EmergencyDeclared, false)]
    [InlineData(IncidentStatus.Reported, IncidentStatus.Resolved, false)]
    [InlineData(IncidentStatus.Confirmed, IncidentStatus.EmergencyDeclared, true)]
    [InlineData(IncidentStatus.Confirmed, IncidentStatus.Resolved, true)]
    [InlineData(IncidentStatus.Confirmed, IncidentStatus.Dismissed, false)]
    [InlineData(IncidentStatus.EmergencyDeclared, IncidentStatus.Resolved, true)]
    [InlineData(IncidentStatus.EmergencyDeclared, IncidentStatus.Closed, false)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Closed, true)]
    [InlineData(IncidentStatus.Dismissed, IncidentStatus.Confirmed, false)]
    [InlineData(IncidentStatus.Dismissed, IncidentStatus.Closed, false)]
    [InlineData(IncidentStatus.Closed, IncidentStatus.Resolved, false)]
    [InlineData(IncidentStatus.Closed, IncidentStatus.Confirmed, false)]
    public void TransitionTableIsExplicit(IncidentStatus from, IncidentStatus to, bool allowed)
    {
        var incident = InState(from);
        Assert.Equal(allowed, incident.CanMoveTo(to));
    }

    [Fact]
    public void TerminalStatesRejectFurtherTransitionsWithoutChangingTheIncident()
    {
        var dismissed = InState(IncidentStatus.Dismissed);
        var changes = dismissed.StatusChanges.Count;

        Assert.Throws<InvalidOperationException>(() => dismissed.Confirm(2, ReportedAt.AddHours(1)));
        Assert.Throws<InvalidOperationException>(() => dismissed.Resolve(2, ReportedAt.AddHours(1)));
        Assert.Equal(IncidentStatus.Dismissed, dismissed.Status);
        Assert.Equal(changes, dismissed.StatusChanges.Count);

        var closed = InState(IncidentStatus.Closed);
        Assert.Throws<InvalidOperationException>(() => closed.DeclareEmergency(1, ReportedAt.AddHours(2)));
        Assert.Equal(IncidentStatus.Closed, closed.Status);
    }

    [Fact]
    public void EvacuationCapturesBelongToTheDeclaredEmergencyWindow()
    {
        var incident = InState(IncidentStatus.EmergencyDeclared);
        var declaredAt = incident.EmergencyDeclaredAt!.Value;

        Assert.False(incident.AcceptsEvacuationCapturedAt(declaredAt.AddMinutes(-5)));
        Assert.True(incident.AcceptsEvacuationCapturedAt(declaredAt.AddMinutes(5)));

        incident.Resolve(2, declaredAt.AddMinutes(30));
        Assert.True(incident.AcceptsEvacuationCapturedAt(declaredAt.AddMinutes(20)));
        Assert.False(incident.AcceptsEvacuationCapturedAt(declaredAt.AddMinutes(45)));

        Assert.False(InState(IncidentStatus.Confirmed).AcceptsEvacuationCapturedAt(ReportedAt.AddMinutes(5)));
        Assert.False(InState(IncidentStatus.Dismissed).AcceptsEvacuationCapturedAt(ReportedAt.AddMinutes(5)));
    }

    [Theory]
    [InlineData("reported", IncidentStatus.Reported)]
    [InlineData("confirmed", IncidentStatus.Confirmed)]
    [InlineData("dismissed", IncidentStatus.Dismissed)]
    [InlineData("emergency_declared", IncidentStatus.EmergencyDeclared)]
    [InlineData("resolved", IncidentStatus.Resolved)]
    [InlineData("closed", IncidentStatus.Closed)]
    [InlineData("inresponse", IncidentStatus.Confirmed)]
    [InlineData("in_response", IncidentStatus.Confirmed)]
    public void StoredAndLegacyCodesAreReadExplicitly(string code, IncidentStatus expected)
    {
        Assert.Equal(expected, IncidentStatusExtensions.FromCode(code));
    }

    [Fact]
    public void CodesAreStableSnakeCaseAndUnknownCodesAreRejected()
    {
        Assert.Equal("emergency_declared", IncidentStatus.EmergencyDeclared.ToCode());
        Assert.Throws<ArgumentException>(() => IncidentStatusExtensions.FromCode("emergencydeclared_typo"));
        Assert.Throws<ArgumentException>(() => IncidentStatusExtensions.FromCode("in-response"));
        // Legacy codes are read case-insensitively after trimming.
        Assert.Equal(IncidentStatus.Confirmed, IncidentStatusExtensions.FromCode(" InResponse "));
    }

    [Fact]
    public void ReportRequiresAServerResolvedGroupAndRejectsFutureReports()
    {
        var command = new DeclareIncidentCommand(1, 11, IncidentType.Medical, IncidentSeverity.High, "Dizziness.",
            ReportedAt);
        Assert.Throws<ArgumentException>(() => new Incident(command, 0, 2, ReportedAt));
        Assert.Throws<ArgumentException>(() =>
            new Incident(command with { ReportedAt = ReportedAt.AddMinutes(10) }, 101, 2, ReportedAt));
        Assert.Throws<ArgumentException>(() => new Incident(command with { AlertId = 0 }, 101, 2, ReportedAt));
    }

    internal static Incident Reported() =>
        new(new DeclareIncidentCommand(1, 11, IncidentType.Accident, IncidentSeverity.High, "Fall near checkpoint 2.",
            ReportedAt), 101, 2, ReportedAt.AddMinutes(1));

    internal static Incident InState(IncidentStatus status)
    {
        var incident = Reported();
        switch (status)
        {
            case IncidentStatus.Reported:
                break;
            case IncidentStatus.Dismissed:
                incident.Dismiss(2, ReportedAt.AddMinutes(5));
                break;
            default:
                incident.Confirm(2, ReportedAt.AddMinutes(5));
                if (status == IncidentStatus.Confirmed) break;
                if (status is IncidentStatus.EmergencyDeclared or IncidentStatus.Closed)
                    incident.DeclareEmergency(1, ReportedAt.AddMinutes(10));
                if (status == IncidentStatus.EmergencyDeclared) break;
                incident.Resolve(2, ReportedAt.AddMinutes(40));
                if (status == IncidentStatus.Closed) incident.Close(1, ReportedAt.AddMinutes(50));
                break;
        }

        return incident;
    }
}
