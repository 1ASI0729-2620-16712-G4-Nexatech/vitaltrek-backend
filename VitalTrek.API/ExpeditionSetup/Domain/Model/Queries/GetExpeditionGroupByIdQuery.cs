using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an expedition group visible to the requester.
/// </summary>
public record GetExpeditionGroupByIdQuery(AccessScope Scope, int ExpeditionGroupId);
