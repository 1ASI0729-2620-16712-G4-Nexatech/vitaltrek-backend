using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="Checkpoint"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class CheckpointRepository(AppDbContext context) : BaseRepository<Checkpoint>(context), ICheckpointRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Checkpoint>> FindByAgencyIdAsync(int agencyId, int? routeId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Checkpoint>()
            .Where(checkpoint => checkpoint.AgencyId == agencyId && (routeId == null || checkpoint.RouteId == routeId))
            .OrderBy(checkpoint => checkpoint.RouteId).ThenBy(checkpoint => checkpoint.SequenceOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Checkpoint?> FindByIdAndAgencyIdAsync(int checkpointId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Checkpoint>().FirstOrDefaultAsync(
            checkpoint => checkpoint.Id == checkpointId && checkpoint.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Checkpoint>> FindByRouteIdAsync(int routeId, CancellationToken cancellationToken = default) =>
        await Context.Set<Checkpoint>().Where(checkpoint => checkpoint.RouteId == routeId)
            .OrderBy(checkpoint => checkpoint.SequenceOrder).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountByRouteIdAsync(int routeId, CancellationToken cancellationToken = default) =>
        await Context.Set<Checkpoint>().CountAsync(checkpoint => checkpoint.RouteId == routeId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByRouteIdAndSequenceOrderAsync(int routeId, int sequenceOrder,
        CancellationToken cancellationToken = default) =>
        await Context.Set<Checkpoint>().AnyAsync(
            checkpoint => checkpoint.RouteId == routeId && checkpoint.SequenceOrder == sequenceOrder, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByBluetoothIdAsync(string bluetoothId, CancellationToken cancellationToken = default)
    {
        var normalized = bluetoothId.Trim().ToUpper();
        return await Context.Set<Checkpoint>().AnyAsync(checkpoint => checkpoint.BluetoothId == normalized, cancellationToken);
    }
}
