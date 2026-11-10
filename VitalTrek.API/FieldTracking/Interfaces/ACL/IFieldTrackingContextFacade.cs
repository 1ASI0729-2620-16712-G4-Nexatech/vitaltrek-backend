namespace VitalTrek.API.FieldTracking.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes Field Tracking information to other bounded contexts
///     without sharing the Field Tracking domain model.
/// </summary>
public interface IFieldTrackingContextFacade
{
    /// <summary>
    ///     Returns the identifier of the active tracking session for an expedition group, or null if none exists.
    /// </summary>
    Task<int?> FindActiveSessionIdByGroupIdAsync(int expeditionGroupId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns true when a tracking session with the given identifier exists.
    /// </summary>
    Task<bool> SessionExistsAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns true when a tracking session with the given identifier exists in the agency.
    /// </summary>
    Task<bool> SessionExistsInAgencyAsync(int agencyId, int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns true when the tracking session exists and is currently active.
    /// </summary>
    Task<bool> IsSessionActiveAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the identifier of the expedition group tracked by a session, or null if the session does not exist.
    /// </summary>
    Task<int?> FindExpeditionGroupIdBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);
}
