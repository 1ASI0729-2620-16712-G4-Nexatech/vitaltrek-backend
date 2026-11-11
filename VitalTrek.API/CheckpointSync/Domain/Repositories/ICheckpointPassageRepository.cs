using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.CheckpointSync.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="CheckpointPassage"/> aggregates.
/// </summary>
public interface ICheckpointPassageRepository : IBaseRepository<CheckpointPassage>
{
    /// <summary>Finds a passage that belongs to an agency.</summary>
    Task<CheckpointPassage?> FindByIdAndAgencyIdAsync(int passageId, int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds the passages of a session in an agency ordered by passage time.</summary>
    Task<IEnumerable<CheckpointPassage>> FindBySessionIdAndAgencyIdAsync(int sessionId, int agencyId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds the passages of a checkpoint in an agency ordered by passage time.</summary>
    Task<IEnumerable<CheckpointPassage>> FindByCheckpointIdAndAgencyIdAsync(int checkpointId, int agencyId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds the passage registered for a checkpoint within a session, if any.</summary>
    Task<CheckpointPassage?> FindBySessionIdAndCheckpointIdAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds the latest passage time registered for a checkpoint within a session, or null if none.</summary>
    Task<DateTimeOffset?> FindLastPassedAtBySessionIdAndCheckpointIdAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default);
}
