using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application command service interface for checkpoint write operations.
/// </summary>
public interface ICheckpointCommandService
{
    /// <summary>Registers a checkpoint in a route.</summary>
    Task<Result<Checkpoint, ECreateCheckpointError>> Handle(CreateCheckpointCommand command, CancellationToken cancellationToken);
}
