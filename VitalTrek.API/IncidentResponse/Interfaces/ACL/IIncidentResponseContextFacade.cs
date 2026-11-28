using VitalTrek.API.IncidentResponse.Interfaces.ACL.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes Incident Response information to other bounded contexts
///     without sharing the Incident Response domain model.
/// </summary>
public interface IIncidentResponseContextFacade
{
    /// <summary>
    ///     Returns how many incidents were declared during a tracking session.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The number of incidents declared during the session.</returns>
    Task<int> CountIncidentsBySessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns true when the session still has an incident that is reported, confirmed or in a declared emergency.
    /// </summary>
    /// <param name="sessionId">The tracking session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when at least one incident of the session is still open.</returns>
    Task<bool> HasOpenIncidentsForSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the counts and open incident identifiers of a session of an agency, computed from persistence, for
    ///     the Tour Closure summary. A session outside the agency produces an empty overview.
    /// </summary>
    /// <param name="agencyId">The agency of the authenticated user.</param>
    /// <param name="sessionId">The tracking session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The overview of the incidents of the session.</returns>
    Task<SessionIncidentOverview> GetSessionIncidentOverviewAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default);
}
