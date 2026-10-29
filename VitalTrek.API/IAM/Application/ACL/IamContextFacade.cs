using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Interfaces.ACL;

namespace VitalTrek.API.IAM.Application.ACL;

/// <summary>
///     Implementation of <see cref="IIamContextFacade"/> backed by the IAM query service.
/// </summary>
/// <param name="userQueryService">The user query service.</param>
public class IamContextFacade(IUserQueryService userQueryService) : IIamContextFacade
{
    /// <inheritdoc />
    public async Task<int> FetchAgencyIdByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId), cancellationToken);
        return user?.AgencyId ?? 0;
    }

    /// <inheritdoc />
    public async Task<int?> FetchFieldGuideIdByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId), cancellationToken);
        return user?.FieldGuideId;
    }

    /// <inheritdoc />
    public async Task<string?> FetchIdentityDocumentByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId), cancellationToken);
        return user?.IdentityDocument;
    }
}
