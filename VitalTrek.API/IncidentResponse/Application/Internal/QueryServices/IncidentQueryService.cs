using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Queries;
using VitalTrek.API.IncidentResponse.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IIncidentQueryService"/>. Administrators see the incidents of
///     their agency, field guides those of their assigned groups and adventure tourists none: incident descriptions may
///     contain health or personal details of other participants.
/// </summary>
/// <param name="incidentRepository">The incident repository.</param>
/// <param name="externalExpeditionSetupService">The outbound service that resolves the group scope of the actor.</param>
public class IncidentQueryService(
    IIncidentRepository incidentRepository,
    ExternalExpeditionSetupService externalExpeditionSetupService) : IIncidentQueryService
{
    /// <inheritdoc />
    public async Task<Incident?> Handle(GetIncidentByIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (!IncidentActorRoles.IsStaff(actor) || actor.AgencyId != query.AgencyId) return null;
        var incident = await incidentRepository.FindByIdAndAgencyIdAsync(query.IncidentId, query.AgencyId,
            cancellationToken);
        return incident is not null &&
               await externalExpeditionSetupService.CanAccessGroup(actor, incident.ExpeditionGroupId, cancellationToken)
            ? incident
            : null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> Handle(GetIncidentsBySessionIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (!IncidentActorRoles.IsStaff(actor) || actor.AgencyId != query.AgencyId) return [];
        var incidents = (await incidentRepository.FindBySessionIdAndAgencyIdAsync(query.SessionId, query.AgencyId,
            cancellationToken)).ToArray();
        if (incidents.Length == 0) return [];
        var groupIds = await externalExpeditionSetupService.FindAccessibleGroupIds(actor, cancellationToken);
        return incidents.Where(incident => groupIds.Contains(incident.ExpeditionGroupId)).ToArray();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Incident>> Handle(GetActiveIncidentsByAgencyIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (!IncidentActorRoles.IsStaff(actor) || actor.AgencyId != query.AgencyId) return [];
        var groupIds = await externalExpeditionSetupService.FindAccessibleGroupIds(actor, cancellationToken);
        return groupIds.Count == 0
            ? []
            : await incidentRepository.FindOpenByAgencyIdAndGroupIdsAsync(query.AgencyId, groupIds, cancellationToken);
    }
}
