using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.IncidentResponse.Interfaces.ACL;

namespace VitalTrek.API.IncidentResponse.Application.ACL;

/// <summary>
///     Implementation of <see cref="IIncidentResponseContextFacade"/> backed by the Incident Response repositories.
/// </summary>
/// <param name="incidentRepository">The incident repository.</param>
public class IncidentResponseContextFacade(IIncidentRepository incidentRepository) : IIncidentResponseContextFacade
{
    /// <inheritdoc />
    public async Task<int> CountIncidentsBySessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await incidentRepository.CountBySessionIdAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasOpenIncidentsForSessionAsync(int sessionId,
        CancellationToken cancellationToken = default) =>
        await incidentRepository.ExistsOpenBySessionIdAsync(sessionId, cancellationToken);
}
