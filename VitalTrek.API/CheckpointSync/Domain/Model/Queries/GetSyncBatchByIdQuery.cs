namespace VitalTrek.API.CheckpointSync.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the persisted result of a sync batch of an agency.
/// </summary>
public record GetSyncBatchByIdQuery(int AgencyId, int BatchId);
