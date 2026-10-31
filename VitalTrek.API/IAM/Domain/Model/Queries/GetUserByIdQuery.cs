namespace VitalTrek.API.IAM.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a user by its identifier.
/// </summary>
/// <param name="Id">The user identifier.</param>
public record GetUserByIdQuery(int Id);
