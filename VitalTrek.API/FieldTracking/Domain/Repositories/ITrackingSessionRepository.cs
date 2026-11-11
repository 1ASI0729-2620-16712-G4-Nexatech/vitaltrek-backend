using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.FieldTracking.Domain.Repositories;

/// <summary>
///     Repository contract for <see cref="TrackingSession"/> persistence.
/// </summary>
public interface ITrackingSessionRepository : IBaseRepository<TrackingSession>
{
    /// <summary>Retrieves all sessions for an agency.</summary>
    Task<IEnumerable<TrackingSession>> FindAllByAgencyIdAsync(int agencyId, CancellationToken ct = default);

    /// <summary>Retrieves a session that belongs to an agency.</summary>
    Task<TrackingSession?> FindByIdAndAgencyIdAsync(int sessionId, int agencyId, CancellationToken ct = default);

    /// <summary>Retrieves the active session for an expedition group, or null if none exists.</summary>
    Task<TrackingSession?> FindActiveByGroupIdAsync(int expeditionGroupId, CancellationToken ct = default);

    /// <summary>Returns true if an active session already exists for the given group.</summary>
    Task<bool> ExistsActiveByGroupIdAsync(int expeditionGroupId, CancellationToken ct = default);
}
