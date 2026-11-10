using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="Incident"/> aggregates.
/// </summary>
public interface IIncidentRepository : IBaseRepository<Incident>
{
    /// <summary>Finds an incident that belongs to an agency.</summary>
    Task<Incident?> FindByIdAndAgencyIdAsync(int incidentId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the incidents of a session in an agency ordered by report time.</summary>
    Task<IEnumerable<Incident>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds the open incidents of an agency ordered by severity and report time.</summary>
    Task<IEnumerable<Incident>> FindOpenByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);
}
