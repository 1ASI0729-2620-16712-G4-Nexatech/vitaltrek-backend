using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Domain.Repositories;

namespace VitalTrek.API.IAM.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IUserQueryService"/>.
/// </summary>
/// <param name="userRepository">The user repository.</param>
public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    /// <inheritdoc />
    public async Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        return await userRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> Handle(GetAllUsersByAgencyIdQuery query, CancellationToken cancellationToken)
    {
        return await userRepository.FindByAgencyIdAsync(query.AgencyId, cancellationToken);
    }
}
