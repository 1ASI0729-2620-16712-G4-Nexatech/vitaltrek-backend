using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="ParticipantPreparation"/> aggregates.
/// </summary>
public interface IParticipantPreparationRepository : IBaseRepository<ParticipantPreparation>
{
    /// <summary>Finds the preparations visible in an access scope, optionally filtered by group.</summary>
    Task<IEnumerable<ParticipantPreparation>> FindByScopeAsync(AccessScope scope, int? expeditionGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a preparation visible in an access scope.</summary>
    Task<ParticipantPreparation?> FindByIdAndScopeAsync(int participantPreparationId, AccessScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether a wearable is assigned to another participant and has not been returned.</summary>
    Task<bool> IsWearableInUseAsync(int wearableId, int excludedParticipantPreparationId, CancellationToken cancellationToken = default);
}
