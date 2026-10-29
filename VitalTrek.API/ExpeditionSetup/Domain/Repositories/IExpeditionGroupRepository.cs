using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="ExpeditionGroup"/> aggregates.
/// </summary>
public interface IExpeditionGroupRepository : IBaseRepository<ExpeditionGroup>
{
    /// <summary>Finds the groups visible in an access scope, optionally filtered by route.</summary>
    Task<IEnumerable<ExpeditionGroup>> FindByScopeAsync(AccessScope scope, int? routeId, CancellationToken cancellationToken = default);

    /// <summary>Finds a group visible in an access scope.</summary>
    Task<ExpeditionGroup?> FindByIdAndScopeAsync(int expeditionGroupId, AccessScope scope, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a field guide is already assigned to another group on the same departure date.</summary>
    Task<bool> HasFieldGuideScheduleConflictAsync(int fieldGuideId, DateOnly departureDate, int excludedExpeditionGroupId,
        CancellationToken cancellationToken = default);
}
