using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for <see cref="WearableDevice"/> aggregates using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public class WearableDeviceRepository(AppDbContext context)
    : BaseRepository<WearableDevice>(context), IWearableDeviceRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<WearableDevice>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default) =>
        await Context.Set<WearableDevice>().Where(device => device.AgencyId == agencyId).OrderBy(device => device.Code)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<WearableDevice?> FindByIdAndAgencyIdAsync(int wearableDeviceId, int agencyId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<WearableDevice>().FirstOrDefaultAsync(
            device => device.Id == wearableDeviceId && device.AgencyId == agencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpper();
        return await Context.Set<WearableDevice>().AnyAsync(device => device.Code == normalized, cancellationToken);
    }
}
