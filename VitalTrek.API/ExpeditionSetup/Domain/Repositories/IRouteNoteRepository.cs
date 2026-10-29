using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="RouteNote"/> aggregates.
/// </summary>
public interface IRouteNoteRepository : IBaseRepository<RouteNote>
{
    /// <summary>Finds the notes of an agency ordered by display order, optionally filtered by route.</summary>
    Task<IEnumerable<RouteNote>> FindByAgencyIdAsync(int agencyId, int? routeId, CancellationToken cancellationToken = default);

    /// <summary>Finds a note that belongs to an agency.</summary>
    Task<RouteNote?> FindByIdAndAgencyIdAsync(int routeNoteId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a route already has a note in the given display position, ignoring one note.</summary>
    Task<bool> ExistsByRouteIdAndDisplayOrderAsync(int routeId, int displayOrder, int? excludedRouteNoteId = null,
        CancellationToken cancellationToken = default);
}
