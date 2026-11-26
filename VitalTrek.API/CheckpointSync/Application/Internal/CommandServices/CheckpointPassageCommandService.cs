using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.CheckpointSync.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="ICheckpointPassageCommandService"/>. The direct REST
///     passage and the passages of a sync batch share the same route, time, sequence and deduplication rules.
/// </summary>
/// <param name="passageRepository">The checkpoint passage repository.</param>
/// <param name="externalFieldTrackingService">The outbound service that resolves the actor's session scope.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class CheckpointPassageCommandService(
    ICheckpointPassageRepository passageRepository,
    ExternalFieldTrackingService externalFieldTrackingService,
    IUnitOfWork unitOfWork,
    ILogger<CheckpointPassageCommandService> logger) : ICheckpointPassageCommandService
{
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> Handle(
        RecordCheckpointPassageCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            if (actor.RoleCode != "field_guide" || actor.AgencyId != command.AgencyId ||
                actor.FieldGuideId != command.FieldGuideId)
                return Fail(ERecordCheckpointPassageError.AccessForbidden);
            var scope = await externalFieldTrackingService.ResolveSessionScope(actor, command.SessionId, cancellationToken);
            if (scope is null) return Fail(ERecordCheckpointPassageError.SessionNotFound);
            if (!scope.IsActive) return Fail(ERecordCheckpointPassageError.SessionNotActive);

            var staged = await StagePassage(command, scope, DateTimeOffset.UtcNow, cancellationToken);
            if (staged is not Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Success
                { Value.WasCreated: true })
                return staged;
            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
            {
                logger.LogWarning(ex, "Concurrent duplicate passage for session {SessionId} and checkpoint {CheckpointId}",
                    command.SessionId, command.CheckpointId);
                // Drop the unsaved passage so the lookup reads the committed winner, not the local copy.
                unitOfWork.DiscardChanges();
                var winner = await passageRepository.FindBySessionIdAndCheckpointIdAsync(command.SessionId,
                    command.CheckpointId, cancellationToken);
                return winner is not null ? Ok(winner, false) : Fail(ERecordCheckpointPassageError.UnexpectedError);
            }

            return staged;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            unitOfWork.DiscardChanges();
            logger.LogError(ex, "Unexpected error recording passage for session {SessionId}", command.SessionId);
            return Fail(ERecordCheckpointPassageError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> StagePassage(
        RecordCheckpointPassageCommand command, SyncSessionScope scope, DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        if (scope.SessionId != command.SessionId) return Fail(ERecordCheckpointPassageError.SessionNotFound);
        if (!scope.ContainsCheckpoint(command.CheckpointId))
            return Fail(ERecordCheckpointPassageError.CheckpointNotInSessionRoute);
        // Invalid evidence is rejected even when the checkpoint already has a passage.
        if (command.PassedAt < scope.StartedAt - ClockTolerance || command.PassedAt > syncedAt + ClockTolerance)
            return Fail(ERecordCheckpointPassageError.InvalidPassedAt);

        CheckpointPassage passage;
        try
        {
            passage = new CheckpointPassage(command, syncedAt);
        }
        catch (ArgumentException ex)
        {
            logger.LogInformation(ex, "Invalid passage staged for session {SessionId}", command.SessionId);
            return Fail(ERecordCheckpointPassageError.InvalidData);
        }

        // The first valid evidence of a checkpoint is kept; a repeated passage never replaces it.
        var existing = await passageRepository.FindBySessionIdAndCheckpointIdAsync(command.SessionId,
            command.CheckpointId, cancellationToken);
        if (existing is not null) return Ok(existing, false);

        // A passage needs the previous route checkpoint passed at or before it, persisted or staged in this
        // unit of work, so a rejected passage never enables the next one.
        var previousCheckpointId = scope.PreviousCheckpointId(command.CheckpointId);
        if (previousCheckpointId is not null)
        {
            var previous = await passageRepository.FindBySessionIdAndCheckpointIdAsync(command.SessionId,
                previousCheckpointId.Value, cancellationToken);
            if (previous is null || previous.PassedAt > command.PassedAt)
                return Fail(ERecordCheckpointPassageError.OutOfSequence);
        }

        await passageRepository.AddAsync(passage, cancellationToken);
        return Ok(passage, true);
    }

    private static Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError> Ok(
        CheckpointPassage passage, bool wasCreated) =>
        new Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Success((passage, wasCreated));

    private static Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError> Fail(
        ERecordCheckpointPassageError error) =>
        new Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Failure(error);
}
