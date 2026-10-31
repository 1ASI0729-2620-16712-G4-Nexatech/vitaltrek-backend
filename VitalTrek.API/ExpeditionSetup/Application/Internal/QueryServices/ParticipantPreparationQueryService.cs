using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>Application query service that implements <see cref="IParticipantPreparationQueryService"/>.</summary>
/// <param name="repository">The repository.</param>
public class ParticipantPreparationQueryService(IParticipantPreparationRepository repository) : IParticipantPreparationQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ParticipantPreparation>> Handle(GetAllParticipantPreparationsQuery query, CancellationToken cancellationToken) =>
        await repository.FindByScopeAsync(query.Scope, query.ExpeditionGroupId, cancellationToken);

    /// <inheritdoc />
    public async Task<ParticipantPreparation?> Handle(GetParticipantPreparationByIdQuery query, CancellationToken cancellationToken) =>
        await repository.FindByIdAndScopeAsync(query.ParticipantPreparationId, query.Scope, cancellationToken);
}
