using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="ParticipantPreparation"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class ParticipantPreparationRepository(AppDbContext context)
    : BaseRepository<ParticipantPreparation>(context), IParticipantPreparationRepository
{
    private IQueryable<ParticipantPreparation> InScope(AccessScope scope)
    {
        var groups = Context.Set<ExpeditionGroup>();
        var manifestEntries = Context.Set<ManifestEntry>();
        return Context.Set<ParticipantPreparation>().Where(preparation => preparation.AgencyId == scope.AgencyId &&
            (scope.FieldGuideId == null || groups.Any(group =>
                group.Id == preparation.ExpeditionGroupId && group.FieldGuideId == scope.FieldGuideId)) &&
            (scope.IdentityDocument == null || manifestEntries.Any(entry =>
                entry.Id == preparation.MemberId && entry.IdentityDocument == scope.IdentityDocument)));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ParticipantPreparation>> FindByScopeAsync(AccessScope scope, int? expeditionGroupId,
        CancellationToken cancellationToken = default) =>
        await InScope(scope)
            .Where(preparation => expeditionGroupId == null || preparation.ExpeditionGroupId == expeditionGroupId)
            .OrderBy(preparation => preparation.ExpeditionGroupId).ThenBy(preparation => preparation.MemberId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ParticipantPreparation?> FindByIdAndScopeAsync(int participantPreparationId, AccessScope scope,
        CancellationToken cancellationToken = default) =>
        await InScope(scope).FirstOrDefaultAsync(preparation => preparation.Id == participantPreparationId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsWearableInUseAsync(int wearableId, int excludedParticipantPreparationId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<ParticipantPreparation>().AnyAsync(preparation => preparation.Id != excludedParticipantPreparationId &&
            preparation.WearableId == wearableId && preparation.ReturnedAt == null, cancellationToken);
}
