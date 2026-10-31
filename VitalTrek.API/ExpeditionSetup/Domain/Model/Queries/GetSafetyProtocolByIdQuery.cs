namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a safety protocol of an agency.
/// </summary>
public record GetSafetyProtocolByIdQuery(int AgencyId, int SafetyProtocolId);
