using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ICheckpointQueryService"/>.
/// </summary>
/// <param name="checkpointRepository">The repository.</param>
public class CheckpointQueryService(ICheckpointRepository checkpointRepository) : ICheckpointQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<Checkpoint>> Handle(GetAllCheckpointsQuery query, CancellationToken cancellationToken) =>
        await checkpointRepository.FindByAgencyIdAsync(query.AgencyId, query.RouteId, cancellationToken);

    /// <inheritdoc />
    public async Task<Checkpoint?> Handle(GetCheckpointByIdQuery query, CancellationToken cancellationToken) =>
        await checkpointRepository.FindByIdAndAgencyIdAsync(query.CheckpointId, query.AgencyId, cancellationToken);
}
