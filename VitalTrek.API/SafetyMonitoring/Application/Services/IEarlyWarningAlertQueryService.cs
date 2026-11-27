using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;

namespace VitalTrek.API.SafetyMonitoring.Application.Services;

/// <summary>
///     Application query service interface for early warning alert read operations. Every read is limited to the
///     groups the authenticated actor can see; an adventure tourist only sees the alerts of their own experience.
/// </summary>
public interface IEarlyWarningAlertQueryService
{
    /// <summary>Retrieves an alert of the agency, or null when it is outside the scope of the actor.</summary>
    Task<EarlyWarningAlert?> Handle(GetAlertByIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Retrieves the open alerts of the groups the actor can see.</summary>
    Task<IEnumerable<EarlyWarningAlert>> Handle(GetActiveAlertsByAgencyIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);

    /// <summary>Retrieves the alerts of a tracking session; empty when the session is outside the scope of the actor.</summary>
    Task<IEnumerable<EarlyWarningAlert>> Handle(GetAlertsBySessionIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);
}
