using VitalTrek.API.FieldTracking.Interfaces.ACL;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that validates tracking session information through the Field Tracking facade.
/// </summary>
/// <param name="fieldTrackingContextFacade">The Field Tracking facade.</param>
public class ExternalTrackingSessionService(IFieldTrackingContextFacade fieldTrackingContextFacade)
{
    /// <summary>Checks whether a tracking session exists in an agency.</summary>
    public async Task<bool> ExistsSession(int agencyId, int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.SessionExistsInAgencyAsync(agencyId, sessionId, cancellationToken);

    /// <summary>Checks whether a tracking session is active.</summary>
    public async Task<bool> IsSessionActive(int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.IsSessionActiveAsync(sessionId, cancellationToken);

    /// <summary>Finds the expedition group tracked by a session.</summary>
    public async Task<int?> FindExpeditionGroupId(int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync(sessionId, cancellationToken);

    /// <summary>Returns the active tracking sessions of every agency, oldest first.</summary>
    /// <param name="maxCount">The maximum number of sessions to return.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    public async Task<IReadOnlyList<ActiveSession>> FindActiveSessions(int maxCount,
        CancellationToken cancellationToken) =>
        (await fieldTrackingContextFacade.FindActiveSessionsAsync(maxCount, cancellationToken))
        .Select(session => new ActiveSession(session.SessionId, session.AgencyId))
        .ToArray();
}

/// <summary>An active tracking session as seen by the periodic safety evaluation.</summary>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="AgencyId">The agency that owns the session.</param>
public sealed record ActiveSession(int SessionId, int AgencyId);
