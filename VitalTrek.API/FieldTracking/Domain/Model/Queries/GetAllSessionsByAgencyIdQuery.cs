namespace VitalTrek.API.FieldTracking.Domain.Model.Queries;

/// <summary>
///     Query to retrieve all tracking sessions that belong to an agency.
/// </summary>
public record GetAllSessionsByAgencyIdQuery(int AgencyId);
