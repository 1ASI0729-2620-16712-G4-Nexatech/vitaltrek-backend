using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application query service interface for expedition group read operations.</summary>
public interface IExpeditionGroupQueryService
{
    /// <summary>Retrieves the expedition group items visible to the requester.</summary>
    Task<IEnumerable<ExpeditionGroup>> Handle(GetAllExpeditionGroupsQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a expedition group item visible to the requester.</summary>
    Task<ExpeditionGroup?> Handle(GetExpeditionGroupByIdQuery query, CancellationToken cancellationToken);
}
