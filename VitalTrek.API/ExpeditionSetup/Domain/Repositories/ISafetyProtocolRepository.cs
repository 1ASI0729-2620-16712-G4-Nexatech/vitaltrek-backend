using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="SafetyProtocol"/> aggregates.
/// </summary>
public interface ISafetyProtocolRepository : IBaseRepository<SafetyProtocol>
{
    /// <summary>Finds the protocols of an agency, optionally filtered by route.</summary>
    Task<IEnumerable<SafetyProtocol>> FindByAgencyIdAsync(int agencyId, int? routeId, CancellationToken cancellationToken = default);

    /// <summary>Finds a protocol that belongs to an agency.</summary>
    Task<SafetyProtocol?> FindByIdAndAgencyIdAsync(int safetyProtocolId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a route already has a safety protocol.</summary>
    Task<bool> ExistsByRouteIdAsync(int routeId, CancellationToken cancellationToken = default);
}
