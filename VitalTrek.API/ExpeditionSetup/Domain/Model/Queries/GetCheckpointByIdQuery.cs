namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a checkpoint of an agency.
/// </summary>
public record GetCheckpointByIdQuery(int AgencyId, int CheckpointId);
