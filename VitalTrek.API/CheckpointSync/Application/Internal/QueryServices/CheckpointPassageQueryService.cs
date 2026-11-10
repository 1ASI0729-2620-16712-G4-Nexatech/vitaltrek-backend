using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Queries;
using VitalTrek.API.CheckpointSync.Domain.Repositories;

namespace VitalTrek.API.CheckpointSync.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ICheckpointPassageQueryService"/>.
/// </summary>
/// <param name="passageRepository">The repository.</param>
public class CheckpointPassageQueryService(ICheckpointPassageRepository passageRepository) : ICheckpointPassageQueryService
{
    /// <inheritdoc />
    public async Task<CheckpointPassage?> Handle(GetPassageByIdQuery query, CancellationToken cancellationToken) =>
        await passageRepository.FindByIdAndAgencyIdAsync(query.PassageId, query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<CheckpointPassage>> Handle(GetPassagesBySessionIdQuery query,
        CancellationToken cancellationToken) =>
        await passageRepository.FindBySessionIdAndAgencyIdAsync(query.SessionId, query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<CheckpointPassage>> Handle(GetPassagesByCheckpointIdQuery query,
        CancellationToken cancellationToken) =>
        await passageRepository.FindByCheckpointIdAndAgencyIdAsync(query.CheckpointId, query.AgencyId, cancellationToken);
}
