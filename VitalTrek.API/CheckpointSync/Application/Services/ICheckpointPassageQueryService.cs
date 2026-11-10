using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Queries;

namespace VitalTrek.API.CheckpointSync.Application.Services;

/// <summary>
///     Application query service interface for checkpoint passage read operations.
/// </summary>
public interface ICheckpointPassageQueryService
{
    /// <summary>Retrieves a passage that belongs to an agency.</summary>
    Task<CheckpointPassage?> Handle(GetPassageByIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the passages of a tracking session.</summary>
    Task<IEnumerable<CheckpointPassage>> Handle(GetPassagesBySessionIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the passages registered at a checkpoint.</summary>
    Task<IEnumerable<CheckpointPassage>> Handle(GetPassagesByCheckpointIdQuery query, CancellationToken cancellationToken);
}
