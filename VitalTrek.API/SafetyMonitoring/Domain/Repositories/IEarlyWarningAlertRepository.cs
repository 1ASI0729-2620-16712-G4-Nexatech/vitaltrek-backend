using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.SafetyMonitoring.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="EarlyWarningAlert"/> aggregates.
/// </summary>
public interface IEarlyWarningAlertRepository : IBaseRepository<EarlyWarningAlert>
{
    /// <summary>Finds an alert that belongs to an agency.</summary>
    Task<EarlyWarningAlert?> FindByIdAndAgencyIdAsync(int alertId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the open (active or acknowledged) alerts of an agency, most recent first.</summary>
    Task<IEnumerable<EarlyWarningAlert>> FindOpenByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the alerts of a session in an agency, most recent first.</summary>
    Task<IEnumerable<EarlyWarningAlert>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether an alert of a type was already raised for a checkpoint within a session.</summary>
    Task<bool> ExistsBySessionIdCheckpointIdAndTypeAsync(int sessionId, int checkpointId, AlertType alertType,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether a session has open (active or acknowledged) alerts.</summary>
    Task<bool> HasOpenBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>Counts the critical alerts of a session that were not dismissed.</summary>
    Task<int> CountCriticalBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);
}
