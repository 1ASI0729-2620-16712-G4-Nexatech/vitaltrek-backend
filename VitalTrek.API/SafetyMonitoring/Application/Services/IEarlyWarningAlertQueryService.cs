using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

namespace VitalTrek.API.SafetyMonitoring.Application.Services;

/// <summary>
///     Application query service interface for early warning alert read operations.
/// </summary>
public interface IEarlyWarningAlertQueryService
{
    /// <summary>Retrieves an alert that belongs to an agency.</summary>
    Task<EarlyWarningAlert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the open alerts of an agency.</summary>
    Task<IEnumerable<EarlyWarningAlert>> Handle(GetActiveAlertsByAgencyIdQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves the alerts of a tracking session.</summary>
    Task<IEnumerable<EarlyWarningAlert>> Handle(GetAlertsBySessionIdQuery query, CancellationToken cancellationToken);
}
