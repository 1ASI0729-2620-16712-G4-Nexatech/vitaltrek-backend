using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application command service interface for incident write operations. Every operation is limited to the groups
///     visible to the authenticated actor and to the role allowed for it.
/// </summary>
public interface IIncidentCommandService
{
    /// <summary>Reports an incident for an active tracking session, manually or from an alert of the same session.</summary>
    Task<Result<Incident, EDeclareIncidentError>> Handle(DeclareIncidentCommand command, AuthenticatedActor actor,
        CancellationToken cancellationToken);

    /// <summary>Executes one explicit transition of the incident lifecycle.</summary>
    Task<Result<Incident, EUpdateIncidentStatusError>> Handle(UpdateIncidentStatusCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken);
}
