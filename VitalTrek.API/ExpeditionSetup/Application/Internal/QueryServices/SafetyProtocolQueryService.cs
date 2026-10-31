using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ISafetyProtocolQueryService"/>.
/// </summary>
/// <param name="safetyProtocolRepository">The repository.</param>
public class SafetyProtocolQueryService(ISafetyProtocolRepository safetyProtocolRepository) : ISafetyProtocolQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<SafetyProtocol>> Handle(GetAllSafetyProtocolsQuery query, CancellationToken cancellationToken) =>
        await safetyProtocolRepository.FindByAgencyIdAsync(query.AgencyId, query.RouteId, cancellationToken);

    /// <inheritdoc />
    public async Task<SafetyProtocol?> Handle(GetSafetyProtocolByIdQuery query, CancellationToken cancellationToken) =>
        await safetyProtocolRepository.FindByIdAndAgencyIdAsync(query.SafetyProtocolId, query.AgencyId, cancellationToken);
}
