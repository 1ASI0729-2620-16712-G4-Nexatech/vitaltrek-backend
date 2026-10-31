using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a participant preparation visible to the requester.
/// </summary>
public record GetParticipantPreparationByIdQuery(AccessScope Scope, int ParticipantPreparationId);
