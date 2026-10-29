using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.IAM.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="User"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().FirstOrDefaultAsync(user => user.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().AnyAsync(user => user.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>()
            .Where(user => user.AgencyId == agencyId)
            .OrderBy(user => user.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByAgencyIdAndFieldGuideIdAsync(int agencyId, int fieldGuideId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>()
            .AnyAsync(user => user.AgencyId == agencyId && user.FieldGuideId == fieldGuideId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByAgencyIdAndIdentityDocumentAsync(int agencyId, string identityDocument,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>()
            .AnyAsync(user => user.AgencyId == agencyId && user.IdentityDocument == identityDocument, cancellationToken);
    }
}
