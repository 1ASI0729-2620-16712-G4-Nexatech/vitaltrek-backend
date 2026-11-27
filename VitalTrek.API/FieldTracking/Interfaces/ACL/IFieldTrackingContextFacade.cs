using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes Field Tracking information to other bounded contexts
///     without sharing the Field Tracking domain model.
/// </summary>
public interface IFieldTrackingContextFacade
{
    /// <summary>Checks access to a safe session projection; tourist membership is resolved by Expedition Setup.</summary>
    Task<bool> CanReadSessionProgressAsync(AuthenticatedActor actor, int sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>Stages one authorized reading without saving. Sync Batch owns the transaction and handles conflicts.</summary>
    Task<TelemetryReadingStageResult> StageTelemetryReadingAsync(AuthenticatedActor actor, int sessionId,
        TelemetryReadingInput reading, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages session completion for Tour Closure. Does not save. The caller must validate returns and stage the
    /// group closure in the same shared unit of work, then save once. Repeated completion preserves the end time.
    /// </summary>
    Task<bool> StageSessionEndAsync(AuthenticatedActor actor, int sessionId, DateTimeOffset endedAt,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    ///     Returns the start time of a tracking session of the agency, or null if it does not exist there.
    ///     Checkpoint Sync uses it to reject passages captured before the session started.
    /// </summary>
    Task<DateTimeOffset?> FindSessionStartedAtAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the persisted readings of a session of the agency with the given client identities. Internal use by
    ///     Checkpoint Sync for the safety evaluation of a batch; returns nothing for a session outside the agency.
    /// </summary>
    Task<IReadOnlyList<TelemetryReadingSnapshot>> FindReadingsAsync(int agencyId, int sessionId,
        IReadOnlyCollection<Guid> clientRecordIds, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the oldest active tracking sessions of every agency, at most <paramref name="maxCount"/> and never
    ///     more than 100. It is an internal lookup for periodic server work, such as the safety evaluation of delays,
    ///     and exposes only identifiers.
    /// </summary>
    /// <param name="maxCount">The maximum number of sessions to return.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The active sessions, oldest first; empty when <paramref name="maxCount"/> is not positive.</returns>
    Task<IReadOnlyList<ActiveTrackingSession>> FindActiveSessionsAsync(int maxCount,
        CancellationToken cancellationToken = default);
}
