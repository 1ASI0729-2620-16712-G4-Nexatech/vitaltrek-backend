using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Application.Errors;
using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.CheckpointSync.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="ICheckpointPassageCommandService"/>.
/// </summary>
/// <param name="passageRepository">The checkpoint passage repository.</param>
/// <param name="externalFieldTrackingService">The outbound service that validates the tracking session.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class CheckpointPassageCommandService(
    ICheckpointPassageRepository passageRepository,
    ExternalFieldTrackingService externalFieldTrackingService,
    IUnitOfWork unitOfWork,
    ILogger<CheckpointPassageCommandService> logger) : ICheckpointPassageCommandService
{
    /// <inheritdoc />
    public async Task<Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>> Handle(
        RecordCheckpointPassageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (!await externalFieldTrackingService.ExistsSession(command.AgencyId, command.SessionId, cancellationToken))
                return Fail(ERecordCheckpointPassageError.SessionNotFound);
            if (!await externalFieldTrackingService.IsSessionActive(command.SessionId, cancellationToken))
                return Fail(ERecordCheckpointPassageError.SessionNotActive);
            if (!await externalFieldTrackingService.ExistsCheckpointInSessionRoute(command.AgencyId, command.SessionId,
                    command.CheckpointId, cancellationToken))
                return Fail(ERecordCheckpointPassageError.CheckpointNotInSessionRoute);

            // Idempotency: the first record of a session and checkpoint is kept.
            var existing = await passageRepository.FindBySessionIdAndCheckpointIdAsync(command.SessionId,
                command.CheckpointId, cancellationToken);
            if (existing is not null) return Ok(existing, false);

            var passage = new CheckpointPassage(command, DateTimeOffset.UtcNow);
            await passageRepository.AddAsync(passage, cancellationToken);
            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
            {
                logger.LogWarning(ex, "Concurrent duplicate passage for session {SessionId} and checkpoint {CheckpointId}",
                    command.SessionId, command.CheckpointId);
                var winner = await passageRepository.FindBySessionIdAndCheckpointIdAsync(command.SessionId,
                    command.CheckpointId, cancellationToken);
                return winner is not null ? Ok(winner, false) : Fail(ERecordCheckpointPassageError.UnexpectedError);
            }

            return Ok(passage, true);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data recording passage for session {SessionId}", command.SessionId);
            return Fail(ERecordCheckpointPassageError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error recording passage for session {SessionId}", command.SessionId);
            return Fail(ERecordCheckpointPassageError.UnexpectedError);
        }
    }

    private static Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError> Ok(
        CheckpointPassage passage, bool wasCreated) =>
        new Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Success((passage, wasCreated));

    private static Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError> Fail(
        ERecordCheckpointPassageError error) =>
        new Result<(CheckpointPassage Passage, bool WasCreated), ERecordCheckpointPassageError>.Failure(error);
}
