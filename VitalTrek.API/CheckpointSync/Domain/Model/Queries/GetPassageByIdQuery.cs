namespace VitalTrek.API.CheckpointSync.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a checkpoint passage of an agency.
/// </summary>
public record GetPassageByIdQuery(int AgencyId, int PassageId);
