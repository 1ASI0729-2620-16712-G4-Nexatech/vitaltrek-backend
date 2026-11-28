using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application query service interface for incident read operations. Administrators read the incidents of their
///     agency and field guides those of their assigned groups; adventure tourists read none.
/// </summary>
public interface IIncidentQueryService
{
    /// <summary>Retrieves an incident visible to the actor.</summary>
    Task<Incident?> Handle(GetIncidentByIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Retrieves the incidents of a tracking session visible to the actor.</summary>
    Task<IEnumerable<Incident>> Handle(GetIncidentsBySessionIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);

    /// <summary>Retrieves the open incidents of the groups visible to the actor.</summary>
    Task<IEnumerable<Incident>> Handle(GetActiveIncidentsByAgencyIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);
}
