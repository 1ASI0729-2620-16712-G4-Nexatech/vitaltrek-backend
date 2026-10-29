namespace VitalTrek.API.IAM.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an agency by its identifier.
/// </summary>
/// <param name="Id">The agency identifier.</param>
public record GetAgencyByIdQuery(int Id);
