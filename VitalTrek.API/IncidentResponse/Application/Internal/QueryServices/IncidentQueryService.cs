using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IIncidentQueryService"/>.
/// </summary>
/// <param name="incidentRepository">The incident repository.</param>
public class IncidentQueryService(IIncidentRepository incidentRepository) : IIncidentQueryService
{
    /// <inheritdoc />
    public async Task<Incident?> Handle(GetIncidentByIdQuery query, CancellationToken cancellationToken) =>
        await incidentRepository.FindByIdAndAgencyIdAsync(query.IncidentId, query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> Handle(GetIncidentsBySessionIdQuery query,
        CancellationToken cancellationToken) =>
        await incidentRepository.FindBySessionIdAndAgencyIdAsync(query.SessionId, query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> Handle(GetActiveIncidentsByAgencyIdQuery query,
        CancellationToken cancellationToken) =>
        await incidentRepository.FindOpenByAgencyIdAsync(query.AgencyId, cancellationToken);
}
