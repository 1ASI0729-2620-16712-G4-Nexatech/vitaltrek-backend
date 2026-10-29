using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="ExpectedTimeWindow"/> aggregates.
/// </summary>
public interface IExpectedTimeWindowRepository : IBaseRepository<ExpectedTimeWindow>
{
    /// <summary>Finds the windows of an agency, optionally filtered by route.</summary>
    Task<IEnumerable<ExpectedTimeWindow>> FindByAgencyIdAsync(int agencyId, int? routeId, CancellationToken cancellationToken = default);

    /// <summary>Finds a window that belongs to an agency.</summary>
    Task<ExpectedTimeWindow?> FindByIdAndAgencyIdAsync(int expectedTimeWindowId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a segment already has a window.</summary>
    Task<bool> ExistsBySegmentAsync(int routeId, int fromCheckpointId, int toCheckpointId, CancellationToken cancellationToken = default);
}
