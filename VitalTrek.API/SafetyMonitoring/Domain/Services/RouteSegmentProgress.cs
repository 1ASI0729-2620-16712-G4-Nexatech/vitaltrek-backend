namespace VitalTrek.API.SafetyMonitoring.Domain.Services;

/// <summary>
///     Determines which route segment can be delayed from the checkpoints a group has confirmed. Expected time windows
///     join consecutive checkpoints, so the segments of a configured route form a single chain from its first checkpoint
///     to its last one. Only the segment that leaves the furthest confirmed checkpoint can be delayed: a segment already
///     completed, or one behind a later confirmed checkpoint, never produces an active delay.
/// </summary>
public static class RouteSegmentProgress
{
    /// <summary>
    ///     Orders the checkpoints of a route from its segments.
    /// </summary>
    /// <param name="segments">The segments of the route, each joining two consecutive checkpoints.</param>
    /// <returns>
    ///     The checkpoints from the first to the last one, or <see langword="null"/> when the segments do not form a
    ///     single chain (a gap, a branch or a cycle).
    /// </returns>
    public static IReadOnlyList<int>? OrderCheckpoints(IReadOnlyCollection<(int FromCheckpointId, int ToCheckpointId)> segments)
    {
        if (segments.Count == 0) return null;
        var next = new Dictionary<int, int>();
        foreach (var (fromCheckpointId, toCheckpointId) in segments)
            if (!next.TryAdd(fromCheckpointId, toCheckpointId)) return null;
        var destinations = segments.Select(segment => segment.ToCheckpointId).ToHashSet();
        var starts = next.Keys.Where(checkpointId => !destinations.Contains(checkpointId)).ToArray();
        if (starts.Length != 1) return null;

        var route = new List<int> { starts[0] };
        while (next.TryGetValue(route[^1], out var following))
        {
            if (route.Contains(following)) return null;
            route.Add(following);
        }
        return route.Count == segments.Count + 1 ? route : null;
    }

    /// <summary>
    ///     Returns the segments whose delay must be evaluated. For a single chain it is, at most, the segment that leaves
    ///     the furthest confirmed checkpoint. When the configuration is not a single chain, every segment whose origin is
    ///     confirmed and whose destination is not is returned, so a delay is never silently ignored.
    /// </summary>
    /// <param name="segments">The segments of the route.</param>
    /// <param name="confirmedCheckpointIds">The checkpoints with a confirmed passage in the session.</param>
    /// <returns>The segments to evaluate; empty when the group has not started the route or already finished it.</returns>
    public static IReadOnlyList<(int FromCheckpointId, int ToCheckpointId)> PendingSegments(
        IReadOnlyCollection<(int FromCheckpointId, int ToCheckpointId)> segments, IReadOnlySet<int> confirmedCheckpointIds)
    {
        var route = OrderCheckpoints(segments);
        if (route is null)
            return segments.Where(segment => confirmedCheckpointIds.Contains(segment.FromCheckpointId) &&
                                             !confirmedCheckpointIds.Contains(segment.ToCheckpointId))
                .ToArray();

        var furthest = -1;
        for (var index = 0; index < route.Count; index++)
            if (confirmedCheckpointIds.Contains(route[index])) furthest = index;
        return furthest < 0 || furthest == route.Count - 1 ? [] : [(route[furthest], route[furthest + 1])];
    }
}
