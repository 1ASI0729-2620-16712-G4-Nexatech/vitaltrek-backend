using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.IncidentResponse.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="Incident"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class IncidentRepository(AppDbContext context)
    : BaseRepository<Incident>(context), IIncidentRepository
{
    private IQueryable<Incident> WithHistory => Context.Set<Incident>().Include(incident => incident.StatusChanges);

    /// <inheritdoc />
    public async Task<Incident?> FindByIdAndAgencyIdAsync(int incidentId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await WithHistory.FirstOrDefaultAsync(
            incident => incident.Id == incidentId && incident.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await WithHistory
            .Where(incident => incident.SessionId == sessionId && incident.AgencyId == agencyId)
            .OrderBy(incident => incident.ReportedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> FindOpenByAgencyIdAndGroupIdsAsync(int agencyId,
        IReadOnlyCollection<int> groupIds, CancellationToken cancellationToken = default)
    {
        var ids = groupIds.ToArray();
        return await WithHistory
            .Where(incident => incident.AgencyId == agencyId && ids.Contains(incident.ExpeditionGroupId) &&
                               (incident.Status == IncidentStatus.Reported ||
                                incident.Status == IncidentStatus.Confirmed ||
                                incident.Status == IncidentStatus.EmergencyDeclared))
            .OrderByDescending(incident => incident.ReportedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Incident?> FindByAlertIdAsync(int agencyId, int alertId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>().FirstOrDefaultAsync(
            incident => incident.AgencyId == agencyId && incident.AlertId == alertId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Incident>> FindAllBySessionIdAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>().AsNoTracking()
            .Where(incident => incident.AgencyId == agencyId && incident.SessionId == sessionId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>().CountAsync(incident => incident.SessionId == sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsOpenBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>().AnyAsync(
            incident => incident.SessionId == sessionId &&
                        (incident.Status == IncidentStatus.Reported ||
                         incident.Status == IncidentStatus.Confirmed ||
                         incident.Status == IncidentStatus.EmergencyDeclared), cancellationToken);
}
