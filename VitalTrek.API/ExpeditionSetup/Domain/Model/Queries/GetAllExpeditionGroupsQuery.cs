using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the expedition groups visible to the requester, optionally filtered by route.
/// </summary>
public record GetAllExpeditionGroupsQuery(AccessScope Scope, int? RouteId);
