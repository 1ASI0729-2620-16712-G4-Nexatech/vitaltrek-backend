using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>Application query service that implements <see cref="IExpeditionGroupQueryService"/>.</summary>
/// <param name="repository">The repository.</param>
public class ExpeditionGroupQueryService(IExpeditionGroupRepository repository) : IExpeditionGroupQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ExpeditionGroup>> Handle(GetAllExpeditionGroupsQuery query, CancellationToken cancellationToken) =>
        await repository.FindByScopeAsync(query.Scope, query.RouteId, cancellationToken);

    /// <inheritdoc />
    public async Task<ExpeditionGroup?> Handle(GetExpeditionGroupByIdQuery query, CancellationToken cancellationToken) =>
        await repository.FindByIdAndScopeAsync(query.ExpeditionGroupId, query.Scope, cancellationToken);
}
