using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.CheckpointSync.Domain.Services;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.CheckpointSync.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="ISyncBatchCommandService"/>. Readings are staged through
///     the Field Tracking telemetry port and passages through <see cref="ICheckpointPassageCommandService"/>; neither
///     saves, so the batch and every accepted record are committed together by a single unit of work.
/// </summary>
/// <param name="syncBatchRepository">The sync batch repository.</param>
/// <param name="passageCommandService">The checkpoint passage command service.</param>
/// <param name="externalFieldTrackingService">The outbound service for sessions and telemetry.</param>
/// <param name="unitOfWork">The unit of work used to commit the batch.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class SyncBatchCommandService(
    ISyncBatchRepository syncBatchRepository,
    ICheckpointPassageCommandService passageCommandService,
    ExternalFieldTrackingService externalFieldTrackingService,
    IUnitOfWork unitOfWork,
    ILogger<SyncBatchCommandService> logger) : ISyncBatchCommandService
{
    private const int MaximumAttempts = 3;

    /// <inheritdoc />
    public async Task<Result<SyncBatch, ESubmitSyncBatchError>> Handle(SubmitSyncBatchCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (actor.RoleCode != "field_guide" || actor.FieldGuideId is not > 0 || actor.AgencyId != command.AgencyId ||
            actor.FieldGuideId != command.FieldGuideId)
            return Fail(ESubmitSyncBatchError.AccessForbidden);
        if (!HasValidShape(command)) return Fail(ESubmitSyncBatchError.InvalidData);
        var fingerprint = SyncBatchFingerprint.Compute(command);

        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            try
            {
                return await Receive(command, actor, fingerprint, cancellationToken);
            }
            catch (Exception ex) when (ex is DbUpdateException update && update.IsDuplicateKeyViolation() ||
                                       ex.IsMySqlTransactionConflict())
            {
                // Another request committed the same batch or record first; the next attempt reads its result.
                logger.LogInformation(ex, "Concurrent sync of batch {ClientBatchId}; rechecking committed state.",
                    command.ClientBatchId);
                unitOfWork.DiscardChanges();
            }
            catch (OperationCanceledException)
            {
                unitOfWork.DiscardChanges();
                throw;
            }
            catch (Exception ex)
            {
                unitOfWork.DiscardChanges();
                logger.LogError(ex, "Unexpected failure receiving sync batch {ClientBatchId}.", command.ClientBatchId);
                return Fail(ESubmitSyncBatchError.UnexpectedError);
            }
        }

        return Fail(ESubmitSyncBatchError.ConcurrentChange);
    }

    private async Task<Result<SyncBatch, ESubmitSyncBatchError>> Receive(SubmitSyncBatchCommand command,
        AuthenticatedActor actor, string fingerprint, CancellationToken cancellationToken)
    {
        var session = await externalFieldTrackingService.ResolveSessionScope(actor, command.SessionId, cancellationToken);
        if (session is null) return Fail(ESubmitSyncBatchError.SessionNotFound);

        // A persisted batch is answered before checking the session state, so a retry after closure still
        // recovers the original result.
        var existing = await syncBatchRepository.FindBySessionIdAndClientBatchIdAsync(command.SessionId,
            command.ClientBatchId, cancellationToken);
        if (existing is not null)
            return existing.HasSameContent(fingerprint)
                ? new Result<SyncBatch, ESubmitSyncBatchError>.Success(existing)
                : Fail(ESubmitSyncBatchError.BatchIdentityConflict);

        if (!session.IsActive) return Fail(ESubmitSyncBatchError.SessionNotActive);
        if (!session.ContainsCheckpoint(command.CheckpointId))
            return Fail(ESubmitSyncBatchError.CheckpointNotInSessionRoute);

        var readingFingerprints = command.Readings.ToDictionary(reading => reading.ClientRecordId,
            SyncBatchFingerprint.ForReading);
        var passageFingerprints = command.Passages.ToDictionary(passage => passage.ClientRecordId,
            SyncBatchFingerprint.ForPassage);
        if (await ReusesIdentityWithDifferentData(command.SessionId, readingFingerprints, passageFingerprints,
                cancellationToken))
            return Fail(ESubmitSyncBatchError.RecordIdentityConflict);

        var batch = new SyncBatch(command, fingerprint, DateTimeOffset.UtcNow);

        // Offline events are processed in capture order, so dependencies are judged by when they happened.
        foreach (var reading in command.Readings.OrderBy(reading => reading.RecordedAt)
                     .ThenBy(reading => reading.ClientRecordId))
        {
            var recordFingerprint = readingFingerprints[reading.ClientRecordId];
            var invalid = SyncReadingRules.RejectionCodeOf(reading, session.StartedAt, batch.ReceivedAt);
            if (invalid is not null)
            {
                batch.Reject(reading.ClientRecordId, SyncRecordKind.Reading, invalid, recordFingerprint);
                continue;
            }

            var staged = await externalFieldTrackingService.StageReading(actor, command.SessionId, reading,
                cancellationToken);
            switch (staged.Status)
            {
                case TelemetryReadingStageStatus.Accepted:
                    batch.Accept(reading.ClientRecordId, SyncRecordKind.Reading, recordFingerprint);
                    break;
                case TelemetryReadingStageStatus.Duplicate:
                    batch.MarkDuplicate(reading.ClientRecordId, SyncRecordKind.Reading, recordFingerprint);
                    break;
                case TelemetryReadingStageStatus.InvalidData:
                case TelemetryReadingStageStatus.WearableNotAssigned:
                case TelemetryReadingStageStatus.ConsentRequired:
                    batch.Reject(reading.ClientRecordId, SyncRecordKind.Reading, ReadingRejectionCode(staged.Status),
                        recordFingerprint);
                    break;
                default:
                    return Abort(ToBatchError(staged.Status));
            }
        }

        foreach (var passage in command.Passages.OrderBy(passage => passage.PassedAt)
                     .ThenBy(passage => session.PositionOf(passage.CheckpointId))
                     .ThenBy(passage => passage.ClientRecordId))
        {
            var recordFingerprint = passageFingerprints[passage.ClientRecordId];
            var staged = await passageCommandService.StagePassage(new RecordCheckpointPassageCommand(command.AgencyId,
                command.FieldGuideId, command.SessionId, passage.CheckpointId, passage.PassedAt), session,
                batch.ReceivedAt, cancellationToken);
            switch (staged)
            {
                case Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Success
                    { Value.WasCreated: true }:
                    batch.Accept(passage.ClientRecordId, SyncRecordKind.Passage, recordFingerprint);
                    break;
                case Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Success:
                    batch.MarkDuplicate(passage.ClientRecordId, SyncRecordKind.Passage, recordFingerprint);
                    break;
                case Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Failure failure
                    when PassageRejectionCode(failure.Error) is { } code:
                    batch.Reject(passage.ClientRecordId, SyncRecordKind.Passage, code, recordFingerprint);
                    break;
                default:
                    return Abort(ESubmitSyncBatchError.UnexpectedError);
            }
        }

        await syncBatchRepository.AddAsync(batch, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return new Result<SyncBatch, ESubmitSyncBatchError>.Success(batch);
    }

    /// <summary>
    ///     A record identity stored or reported as duplicate by an earlier batch must keep its content. Rejected
    ///     results do not count, so a corrected record may be sent again with the same identity.
    /// </summary>
    private async Task<bool> ReusesIdentityWithDifferentData(int sessionId,
        IReadOnlyDictionary<Guid, string> readingFingerprints, IReadOnlyDictionary<Guid, string> passageFingerprints,
        CancellationToken cancellationToken)
    {
        var identities = readingFingerprints.Keys.Concat(passageFingerprints.Keys).ToArray();
        var earlier = await syncBatchRepository.FindNonRejectedRecordsAsync(sessionId, identities, cancellationToken);
        return earlier.Any(record =>
        {
            var current = readingFingerprints.TryGetValue(record.ClientRecordId, out var reading)
                ? reading
                : passageFingerprints[record.ClientRecordId];
            return !string.Equals(record.RecordFingerprint, current, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool HasValidShape(SubmitSyncBatchCommand command)
    {
        if (command.ClientBatchId == Guid.Empty || command.SessionId <= 0 || command.CheckpointId <= 0 ||
            command.Readings is null || command.Passages is null)
            return false;
        var total = command.Readings.Count + command.Passages.Count;
        if (total is < 1 or > SyncBatch.MaximumRecords) return false;
        if (command.Readings.Any(reading => reading is null) || command.Passages.Any(passage => passage is null))
            return false;
        var identities = command.Readings.Select(reading => reading.ClientRecordId)
            .Concat(command.Passages.Select(passage => passage.ClientRecordId)).ToArray();
        return identities.All(identity => identity != Guid.Empty) && identities.Distinct().Count() == identities.Length;
    }

    private static string ReadingRejectionCode(TelemetryReadingStageStatus status) => status switch
    {
        TelemetryReadingStageStatus.WearableNotAssigned => SyncRejectionCodes.TelemetryWearableNotAssigned,
        TelemetryReadingStageStatus.ConsentRequired => SyncRejectionCodes.TelemetryConsentRequired,
        _ => SyncRejectionCodes.TelemetryInvalidData
    };

    private static string? PassageRejectionCode(ERecordCheckpointPassageError error) => error switch
    {
        ERecordCheckpointPassageError.CheckpointNotInSessionRoute => SyncRejectionCodes.PassageCheckpointNotInRoute,
        ERecordCheckpointPassageError.InvalidPassedAt or ERecordCheckpointPassageError.InvalidData =>
            SyncRejectionCodes.PassageInvalidPassedAt,
        ERecordCheckpointPassageError.OutOfSequence => SyncRejectionCodes.PassageOutOfSequence,
        _ => null
    };

    private static ESubmitSyncBatchError ToBatchError(TelemetryReadingStageStatus status) => status switch
    {
        TelemetryReadingStageStatus.AccessForbidden => ESubmitSyncBatchError.AccessForbidden,
        TelemetryReadingStageStatus.SessionNotFound => ESubmitSyncBatchError.SessionNotFound,
        TelemetryReadingStageStatus.SessionNotActive => ESubmitSyncBatchError.SessionNotActive,
        TelemetryReadingStageStatus.RecordIdentityConflict => ESubmitSyncBatchError.RecordIdentityConflict,
        TelemetryReadingStageStatus.ConcurrentChange => ESubmitSyncBatchError.ConcurrentChange,
        _ => ESubmitSyncBatchError.UnexpectedError
    };

    private Result<SyncBatch, ESubmitSyncBatchError> Abort(ESubmitSyncBatchError error)
    {
        // Nothing staged by this batch may reach a later save of the same scope.
        unitOfWork.DiscardChanges();
        return Fail(error);
    }

    private static Result<SyncBatch, ESubmitSyncBatchError> Fail(ESubmitSyncBatchError error) =>
        new Result<SyncBatch, ESubmitSyncBatchError>.Failure(error);
}
