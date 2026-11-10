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
    /// <inheritdoc />
    public async Task<Incident?> FindByIdAndAgencyIdAsync(int incidentId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>().FirstOrDefaultAsync(
            incident => incident.Id == incidentId && incident.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>()
            .Where(incident => incident.SessionId == sessionId && incident.AgencyId == agencyId)
            .OrderBy(incident => incident.ReportedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> FindOpenByAgencyIdAsync(int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Incident>()
            .Where(incident => incident.AgencyId == agencyId &&
                               (incident.Status == IncidentStatus.Reported ||
                                incident.Status == IncidentStatus.InResponse))
            .OrderByDescending(incident => incident.ReportedAt)
            .ToListAsync(cancellationToken);
}
