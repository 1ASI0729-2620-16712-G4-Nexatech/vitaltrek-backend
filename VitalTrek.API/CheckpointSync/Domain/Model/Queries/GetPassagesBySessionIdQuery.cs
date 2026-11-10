namespace VitalTrek.API.CheckpointSync.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the checkpoint passages of a tracking session.
/// </summary>
public record GetPassagesBySessionIdQuery(int AgencyId, int SessionId);
