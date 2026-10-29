using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Queries;

namespace VitalTrek.API.IAM.Application.Services;

/// <summary>
///     Application query service interface for user account read operations.
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    ///     Retrieves a user by its identifier.
    /// </summary>
    /// <param name="query">The query containing the user identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The user if found; otherwise, <see langword="null"/>.</returns>
    Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves all the users of an agency.
    /// </summary>
    /// <param name="query">The query containing the agency identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The users of the agency.</returns>
    Task<IEnumerable<User>> Handle(GetAllUsersByAgencyIdQuery query, CancellationToken cancellationToken);
}
