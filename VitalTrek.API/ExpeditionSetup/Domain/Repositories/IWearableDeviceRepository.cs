using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="WearableDevice"/> aggregates.
/// </summary>
public interface IWearableDeviceRepository : IBaseRepository<WearableDevice>
{
    /// <summary>Finds the wearable inventory of an agency.</summary>
    Task<IEnumerable<WearableDevice>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds a wearable device that belongs to an agency.</summary>
    Task<WearableDevice?> FindByIdAndAgencyIdAsync(int wearableDeviceId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an inventory code is already registered.</summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
}
