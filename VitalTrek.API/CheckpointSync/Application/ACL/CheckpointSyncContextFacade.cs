using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.CheckpointSync.Interfaces.ACL.Resources;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.CheckpointSync.Application.ACL;

/// <summary>
///     Implementation of <see cref="ICheckpointSyncContextFacade"/> backed by the Checkpoint Sync repositories.
/// </summary>
/// <param name="passageRepository">The checkpoint passage repository.</param>
public class CheckpointSyncContextFacade(ICheckpointPassageRepository passageRepository,
    IFieldTrackingContextFacade fieldTrackingContextFacade) : ICheckpointSyncContextFacade
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfirmedCheckpointPassage>> FindConfirmedPassagesAsync(AuthenticatedActor actor,
        int sessionId, CancellationToken cancellationToken = default)
    {
        if (!await fieldTrackingContextFacade.CanReadSessionProgressAsync(actor, sessionId, cancellationToken)) return [];
        var passages = await passageRepository.FindBySessionIdAndAgencyIdAsync(sessionId, actor.AgencyId, cancellationToken);
        return passages.Where(passage => passage.Status is CheckpointPassageStatus.Synced or CheckpointPassageStatus.DelayedSync)
            .Select(passage => new ConfirmedCheckpointPassage(passage.CheckpointId, passage.FieldGuideId,
                passage.PassedAt, passage.SyncedAt)).ToArray();
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetLastPassageTimeAtCheckpointBySessionAsync(int sessionId, int checkpointId,
        CancellationToken cancellationToken = default) =>
        await passageRepository.FindLastPassedAtBySessionIdAndCheckpointIdAsync(sessionId, checkpointId, cancellationToken);
}
