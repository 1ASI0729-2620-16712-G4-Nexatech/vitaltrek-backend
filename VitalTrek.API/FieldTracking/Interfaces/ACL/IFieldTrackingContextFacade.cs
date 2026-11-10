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
}
