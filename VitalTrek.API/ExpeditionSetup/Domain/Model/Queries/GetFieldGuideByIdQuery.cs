namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a field guide of an agency.
/// </summary>
public record GetFieldGuideByIdQuery(int AgencyId, int FieldGuideId);
