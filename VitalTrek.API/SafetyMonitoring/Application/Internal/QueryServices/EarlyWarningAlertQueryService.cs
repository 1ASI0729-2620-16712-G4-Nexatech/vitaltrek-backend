using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.SafetyMonitoring.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.SafetyMonitoring.Application.Services;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Queries;
using VitalTrek.API.SafetyMonitoring.Domain.Repositories;

namespace VitalTrek.API.SafetyMonitoring.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IEarlyWarningAlertQueryService"/>. Administrators see the
///     alerts of their agency, field guides those of their assigned groups and adventure tourists the delays of their
///     group and the alerts about themselves.
/// </summary>
/// <param name="alertRepository">The early warning alert repository.</param>
/// <param name="externalRouteSetupService">The outbound service that resolves the group scope of the actor.</param>
public class EarlyWarningAlertQueryService(
    IEarlyWarningAlertRepository alertRepository,
    ExternalRouteSetupService externalRouteSetupService) : IEarlyWarningAlertQueryService
{
    // Role code published by the IAM authenticated actor contract.
    private const string AdventureTouristRoleCode = "adventure_tourist";

    /// <inheritdoc />
    public async Task<EarlyWarningAlert?> Handle(GetAlertByIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (actor.AgencyId != query.AgencyId) return null;
        var alert = await alertRepository.FindByIdAndAgencyIdAsync(query.AlertId, query.AgencyId, cancellationToken);
        if (alert is null || !await externalRouteSetupService.CanAccessGroup(actor, alert.ExpeditionGroupId,
                cancellationToken))
            return null;
        return (await VisibleTo(actor, [alert], cancellationToken)).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EarlyWarningAlert>> Handle(GetActiveAlertsByAgencyIdQuery query,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (actor.AgencyId != query.AgencyId) return [];
        var groupIds = await externalRouteSetupService.FindAccessibleGroupIds(actor, cancellationToken);
        if (groupIds.Count == 0) return [];
        var alerts = await alertRepository.FindOpenByAgencyIdAndGroupIdsAsync(query.AgencyId, groupIds,
            cancellationToken);
        return await VisibleTo(actor, alerts.ToArray(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EarlyWarningAlert>> Handle(GetAlertsBySessionIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (actor.AgencyId != query.AgencyId) return [];
        var alerts = (await alertRepository.FindBySessionIdAndAgencyIdAsync(query.SessionId, query.AgencyId,
            cancellationToken)).ToArray();
        // Every alert of a session belongs to the group tracked by that session.
        if (alerts.Length == 0 || !await externalRouteSetupService.CanAccessGroup(actor, alerts[0].ExpeditionGroupId,
                cancellationToken))
            return [];
        return await VisibleTo(actor, alerts, cancellationToken);
    }

    private async Task<IReadOnlyList<EarlyWarningAlert>> VisibleTo(AuthenticatedActor actor,
        IReadOnlyList<EarlyWarningAlert> alerts, CancellationToken cancellationToken)
    {
        if (actor.RoleCode != AdventureTouristRoleCode) return alerts;
        var visible = new List<EarlyWarningAlert>();
        foreach (var group in alerts.GroupBy(alert => alert.ExpeditionGroupId))
        {
            var ownMemberIds = await externalRouteSetupService.FindVisibleMemberIds(actor, group.Key, cancellationToken);
            visible.AddRange(group.Where(alert => alert.IsVisibleToParticipant(ownMemberIds)));
        }
        return alerts.Where(visible.Contains).ToArray();
    }
}
