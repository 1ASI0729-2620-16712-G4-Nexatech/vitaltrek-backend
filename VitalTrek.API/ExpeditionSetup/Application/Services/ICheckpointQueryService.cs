using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;


namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application query service interface for checkpoint read operations.
/// </summary>
public interface ICheckpointQueryService
{
    /// <summary>Retrieves the checkpoints of an agency, optionally filtered by route.</summary>
    Task<IEnumerable<Checkpoint>> Handle(GetAllCheckpointsQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a checkpoint that belongs to an agency.</summary>
    Task<Checkpoint?> Handle(GetCheckpointByIdQuery query, CancellationToken cancellationToken);
}
