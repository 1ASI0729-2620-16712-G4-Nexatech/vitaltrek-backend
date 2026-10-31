namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an expected time window of an agency.
/// </summary>
public record GetExpectedTimeWindowByIdQuery(int AgencyId, int ExpectedTimeWindowId);
