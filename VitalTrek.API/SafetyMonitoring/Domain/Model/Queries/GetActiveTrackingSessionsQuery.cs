namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the active tracking sessions whose delays must be evaluated periodically, oldest first.
/// </summary>
/// <param name="MaxCount">The maximum number of sessions to return.</param>
public record GetActiveTrackingSessionsQuery(int MaxCount);
