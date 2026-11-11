using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application command service interface for incident write operations.
/// </summary>
public interface IIncidentCommandService
{
    /// <summary>Declares an incident for an active tracking session.</summary>
    Task<Result<Incident, EDeclareIncidentError>> Handle(DeclareIncidentCommand command,
        CancellationToken cancellationToken);

    /// <summary>Moves an incident to another state of its lifecycle.</summary>
    Task<Result<Incident, EUpdateIncidentStatusError>> Handle(UpdateIncidentStatusCommand command,
        CancellationToken cancellationToken);

    /// <summary>Resolves an open incident.</summary>
    Task<Result<Incident, EResolveIncidentError>> Handle(ResolveIncidentCommand command,
        CancellationToken cancellationToken);
}
