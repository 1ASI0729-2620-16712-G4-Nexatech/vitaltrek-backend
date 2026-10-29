using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="SafetyProtocol"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class SafetyProtocolRepository(AppDbContext context)
    : BaseRepository<SafetyProtocol>(context), ISafetyProtocolRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<SafetyProtocol>> FindByAgencyIdAsync(int agencyId, int? routeId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<SafetyProtocol>()
            .Where(protocol => protocol.AgencyId == agencyId && (routeId == null || protocol.RouteId == routeId))
            .OrderBy(protocol => protocol.RouteId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<SafetyProtocol?> FindByIdAndAgencyIdAsync(int safetyProtocolId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<SafetyProtocol>().FirstOrDefaultAsync(
            protocol => protocol.Id == safetyProtocolId && protocol.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByRouteIdAsync(int routeId, CancellationToken cancellationToken = default) =>
        await Context.Set<SafetyProtocol>().AnyAsync(protocol => protocol.RouteId == routeId, cancellationToken);
}
