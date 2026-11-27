using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IEvacuationActionQueryService"/>. The actions are visible to
///     whoever can see their incident.
/// </summary>
/// <param name="evacuationActionRepository">The evacuation action repository.</param>
/// <param name="incidentQueryService">The incident query service that applies the group scope.</param>
public class EvacuationActionQueryService(
    IEvacuationActionRepository evacuationActionRepository,
    IIncidentQueryService incidentQueryService) : IEvacuationActionQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EvacuationAction>?> Handle(GetEvacuationActionsByIncidentIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        var incident = await incidentQueryService.Handle(new GetIncidentByIdQuery(query.AgencyId, query.IncidentId),
            actor, cancellationToken);
        if (incident is null) return null;
        return (await evacuationActionRepository.FindByIncidentIdAsync(query.AgencyId, incident.Id, cancellationToken))
            .ToArray();
    }
}
