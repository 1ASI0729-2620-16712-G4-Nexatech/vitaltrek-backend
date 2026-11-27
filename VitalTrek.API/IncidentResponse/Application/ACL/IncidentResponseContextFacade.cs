using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.IncidentResponse.Interfaces.ACL;
using VitalTrek.API.IncidentResponse.Interfaces.ACL.Resources;

namespace VitalTrek.API.IncidentResponse.Application.ACL;

/// <summary>
///     Implementation of <see cref="IIncidentResponseContextFacade"/> backed by the Incident Response repositories.
/// </summary>
/// <param name="incidentRepository">The incident repository.</param>
/// <param name="evacuationActionRepository">The evacuation action repository.</param>
public class IncidentResponseContextFacade(
    IIncidentRepository incidentRepository,
    IEvacuationActionRepository evacuationActionRepository) : IIncidentResponseContextFacade
{
    /// <inheritdoc />
    public async Task<int> CountIncidentsBySessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await incidentRepository.CountBySessionIdAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasOpenIncidentsForSessionAsync(int sessionId,
        CancellationToken cancellationToken = default) =>
        await incidentRepository.ExistsOpenBySessionIdAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<SessionIncidentOverview> GetSessionIncidentOverviewAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default)
    {
        if (agencyId <= 0 || sessionId <= 0) return new SessionIncidentOverview(0, 0, 0, 0, 0, []);
        var incidents = await incidentRepository.FindAllBySessionIdAsync(agencyId, sessionId, cancellationToken);
        var actions = incidents.Count == 0
            ? 0
            : await evacuationActionRepository.CountByIncidentIdsAsync(agencyId,
                incidents.Select(incident => incident.Id).ToArray(), cancellationToken);
        return new SessionIncidentOverview(
            incidents.Count,
            incidents.Count(incident => incident.IsOpen),
            incidents.Count(incident => incident.EmergencyDeclaredAt is not null),
            incidents.Count(incident => incident.Status == IncidentStatus.Dismissed),
            actions,
            incidents.Where(incident => incident.IsOpen).OrderBy(incident => incident.ReportedAt)
                .Select(incident => incident.Id).ToArray());
    }
}
