namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the persisted sync batches whose safety evaluation is still pending, oldest first.
/// </summary>
/// <param name="MaxCount">The maximum number of batches to return.</param>
public record GetPendingSyncBatchEvaluationsQuery(int MaxCount);
