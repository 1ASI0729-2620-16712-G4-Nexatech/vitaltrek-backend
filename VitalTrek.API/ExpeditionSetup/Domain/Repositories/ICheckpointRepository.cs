using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="Checkpoint"/> aggregates.
/// </summary>
public interface ICheckpointRepository : IBaseRepository<Checkpoint>
{
    /// <summary>Finds the checkpoints of an agency ordered by route and sequence, optionally filtered by route.</summary>
    Task<IEnumerable<Checkpoint>> FindByAgencyIdAsync(int agencyId, int? routeId, CancellationToken cancellationToken = default);

    /// <summary>Finds a checkpoint that belongs to an agency.</summary>
    Task<Checkpoint?> FindByIdAndAgencyIdAsync(int checkpointId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the checkpoints of a route ordered by sequence.</summary>
    Task<IReadOnlyList<Checkpoint>> FindByRouteIdAsync(int routeId, CancellationToken cancellationToken = default);

    /// <summary>Counts the checkpoints of a route.</summary>
    Task<int> CountByRouteIdAsync(int routeId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a route already has a checkpoint in the given position.</summary>
    Task<bool> ExistsByRouteIdAndSequenceOrderAsync(int routeId, int sequenceOrder, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a Bluetooth identifier is already registered.</summary>
    Task<bool> ExistsByBluetoothIdAsync(string bluetoothId, CancellationToken cancellationToken = default);
}
