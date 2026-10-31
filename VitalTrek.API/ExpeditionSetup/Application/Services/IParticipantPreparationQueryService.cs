using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application query service interface for participant preparation read operations.</summary>
public interface IParticipantPreparationQueryService
{
    /// <summary>Retrieves the participant preparation items visible to the requester.</summary>
    Task<IEnumerable<ParticipantPreparation>> Handle(GetAllParticipantPreparationsQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a participant preparation item visible to the requester.</summary>
    Task<ParticipantPreparation?> Handle(GetParticipantPreparationByIdQuery query, CancellationToken cancellationToken);
}
