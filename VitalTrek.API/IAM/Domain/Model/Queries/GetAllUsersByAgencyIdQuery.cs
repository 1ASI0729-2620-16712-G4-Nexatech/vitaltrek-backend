namespace VitalTrek.API.IAM.Domain.Model.Queries;

/// <summary>
///     Query to retrieve all the user accounts of an agency.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
public record GetAllUsersByAgencyIdQuery(int AgencyId);
