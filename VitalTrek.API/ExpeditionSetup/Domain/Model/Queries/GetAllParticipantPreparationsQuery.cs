using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the participant preparations visible to the requester, optionally filtered by group.
/// </summary>
public record GetAllParticipantPreparationsQuery(AccessScope Scope, int? ExpeditionGroupId);
