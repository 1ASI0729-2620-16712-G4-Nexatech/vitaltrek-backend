namespace VitalTrek.API.FieldTracking.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the currently active tracking session for an expedition group.
/// </summary>
public record GetActiveSessionByGroupIdQuery(int AgencyId, int ExpeditionGroupId);
