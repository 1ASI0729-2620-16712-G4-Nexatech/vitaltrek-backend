using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application query service interface for evacuation actions.
/// </summary>
public interface IEvacuationActionQueryService
{
    /// <summary>
    ///     Retrieves the evacuation actions of an incident visible to the actor, or null when the incident is not visible.
    /// </summary>
    Task<IReadOnlyList<EvacuationAction>?> Handle(GetEvacuationActionsByIncidentIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);
}
