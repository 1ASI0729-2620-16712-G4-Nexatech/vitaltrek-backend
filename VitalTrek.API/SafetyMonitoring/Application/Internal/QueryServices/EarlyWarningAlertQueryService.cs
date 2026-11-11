using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IEarlyWarningAlertQueryService"/>.
/// </summary>
/// <param name="alertRepository">The early warning alert repository.</param>
public class EarlyWarningAlertQueryService(IEarlyWarningAlertRepository alertRepository) : IEarlyWarningAlertQueryService
{
    /// <inheritdoc />
    public async Task<EarlyWarningAlert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken) =>
        await alertRepository.FindByIdAndAgencyIdAsync(query.AlertId, query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<EarlyWarningAlert>> Handle(GetActiveAlertsByAgencyIdQuery query,
        CancellationToken cancellationToken) =>
        await alertRepository.FindOpenByAgencyIdAsync(query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<EarlyWarningAlert>> Handle(GetAlertsBySessionIdQuery query,
        CancellationToken cancellationToken) =>
        await alertRepository.FindBySessionIdAndAgencyIdAsync(query.SessionId, query.AgencyId, cancellationToken);
}
