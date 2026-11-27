using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="Incident"/> aggregates. Incidents are always loaded with their
///     recorded transitions.
/// </summary>
public interface IIncidentRepository : IBaseRepository<Incident>
{
    /// <summary>Finds an incident that belongs to an agency.</summary>
    Task<Incident?> FindByIdAndAgencyIdAsync(int incidentId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the incidents of a session in an agency ordered by report time.</summary>
    Task<IEnumerable<Incident>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds the open incidents of the given groups of an agency, most recent first.</summary>
    Task<IEnumerable<Incident>> FindOpenByAgencyIdAndGroupIdsAsync(int agencyId, IReadOnlyCollection<int> groupIds,
        CancellationToken cancellationToken = default);

    /// <summary>Finds the incident linked to an early warning alert of an agency.</summary>
    Task<Incident?> FindByAlertIdAsync(int agencyId, int alertId, CancellationToken cancellationToken = default);

    /// <summary>Finds every incident of a session of an agency, without the transition history.</summary>
    Task<IReadOnlyList<Incident>> FindAllBySessionIdAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Counts the incidents declared during a tracking session.</summary>
    Task<int> CountBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when the session has an incident that is still open.</summary>
    Task<bool> ExistsOpenBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);
}
