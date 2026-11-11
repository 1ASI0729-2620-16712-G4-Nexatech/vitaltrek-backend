using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>
///     Application query service interface for incident read operations.
/// </summary>
public interface IIncidentQueryService
{
    /// <summary>Retrieves an incident that belongs to an agency.</summary>
    Task<Incident?> Handle(GetIncidentByIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the incidents declared during a tracking session.</summary>
    Task<IEnumerable<Incident>> Handle(GetIncidentsBySessionIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the incidents of the agency that are still open.</summary>
    Task<IEnumerable<Incident>> Handle(GetActiveIncidentsByAgencyIdQuery query, CancellationToken cancellationToken);
}
