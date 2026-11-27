using VitalTrek.API.SafetyMonitoring.Domain.Services;
using Xunit;

namespace VitalTrek.API.Tests.SafetyMonitoring.Domain;

/// <summary>Only the segment that leaves the furthest confirmed checkpoint can be delayed.</summary>
public class RouteSegmentProgressTests
{
    // Unordered on purpose: the route is ordered from the chain of consecutive segments.
    private static readonly (int FromCheckpointId, int ToCheckpointId)[] Route = [(2, 3), (3, 4), (1, 2)];

    [Fact]
    public void OrdersTheCheckpointsFromTheChainOfSegments()
    {
        Assert.Equal(new[] { 1, 2, 3, 4 }, RouteSegmentProgress.OrderCheckpoints(Route));
    }

    [Theory]
    [MemberData(nameof(NotASingleChain))]
    public void SegmentsThatAreNotASingleChainCannotBeOrdered((int, int)[] segments)
    {
        Assert.Null(RouteSegmentProgress.OrderCheckpoints(segments));
    }

    public static TheoryData<(int, int)[]> NotASingleChain() => new()
    {
        new[] { (1, 2), (3, 4) },
        new[] { (1, 2), (1, 3) },
        new[] { (1, 2), (2, 1) },
        Array.Empty<(int, int)>()
    };

    [Theory]
    [InlineData(new[] { 1 }, 1, 2)]
    [InlineData(new[] { 1, 2 }, 2, 3)]
    [InlineData(new[] { 1, 3 }, 3, 4)]
    public void TheNextPendingSegmentLeavesTheFurthestConfirmedCheckpoint(int[] confirmed, int from, int to)
    {
        var pending = Assert.Single(RouteSegmentProgress.PendingSegments(Route, confirmed.ToHashSet()));
        Assert.Equal((from, to), pending);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 2, 3, 4 })]
    [InlineData(new[] { 4 })]
    public void ARouteNotStartedOrAlreadyFinishedHasNoPendingSegment(int[] confirmed)
    {
        Assert.Empty(RouteSegmentProgress.PendingSegments(Route, confirmed.ToHashSet()));
    }

    [Fact]
    public void ABrokenChainStillEvaluatesEverySegmentStartedAndNotFinished()
    {
        (int, int)[] broken = [(1, 2), (3, 4)];
        Assert.Equal(new[] { (1, 2), (3, 4) }, RouteSegmentProgress.PendingSegments(broken, new HashSet<int> { 1, 3 }));
    }
}
