namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to evaluate, on the server, the readings accepted by a persisted sync batch.
/// </summary>
/// <param name="AgencyId">The agency that owns the batch.</param>
/// <param name="BatchId">The sync batch identifier.</param>
public record EvaluateSyncBatchCommand(int AgencyId, int BatchId);
