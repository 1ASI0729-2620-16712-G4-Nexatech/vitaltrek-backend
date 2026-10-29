using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="ExpectedTimeWindow"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class ExpectedTimeWindowRepository(AppDbContext context)
    : BaseRepository<ExpectedTimeWindow>(context), IExpectedTimeWindowRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<ExpectedTimeWindow>> FindByAgencyIdAsync(int agencyId, int? routeId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<ExpectedTimeWindow>()
            .Where(window => window.AgencyId == agencyId && (routeId == null || window.RouteId == routeId))
            .OrderBy(window => window.RouteId).ThenBy(window => window.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ExpectedTimeWindow?> FindByIdAndAgencyIdAsync(int expectedTimeWindowId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<ExpectedTimeWindow>().FirstOrDefaultAsync(
            window => window.Id == expectedTimeWindowId && window.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsBySegmentAsync(int routeId, int fromCheckpointId, int toCheckpointId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<ExpectedTimeWindow>().AnyAsync(window => window.RouteId == routeId &&
            window.FromCheckpointId == fromCheckpointId && window.ToCheckpointId == toCheckpointId, cancellationToken);
}
