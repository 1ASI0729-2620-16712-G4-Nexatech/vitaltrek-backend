using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.IncidentResponse.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="IncidentNotification"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class IncidentNotificationRepository(AppDbContext context)
    : BaseRepository<IncidentNotification>(context), IIncidentNotificationRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<IncidentNotification>> FindByIncidentIdAndAgencyIdAsync(int incidentId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<IncidentNotification>()
            .Where(notification => notification.IncidentId == incidentId && notification.AgencyId == agencyId)
            .OrderBy(notification => notification.SentAt).ToListAsync(cancellationToken);
}
