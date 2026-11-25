using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.FieldTracking.Application.Internal.CommandServices;

/// <summary>Starts the group and its session in the shared unit of work.</summary>
public class TrackingSessionCommandService(
    ITrackingSessionRepository trackingSessionRepository,
    IExpeditionSetupContextFacade expeditionSetupContextFacade,
    IUnitOfWork unitOfWork,
    ILogger<TrackingSessionCommandService> logger) : ITrackingSessionCommandService
{
    public async Task<Result<TrackingSessionStartResult, EStartTrackingSessionError>> Handle(
        StartTrackingSessionCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanWrite(actor, command.AgencyId))
            return Failure(EStartTrackingSessionError.AccessForbidden);
        if (command.ExpeditionGroupId <= 0) return Failure(EStartTrackingSessionError.InvalidData);

        // Another request may win the start. Discard the failed attempt and read committed state again.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor, command.ExpeditionGroupId,
                        cancellationToken))
                    return Failure(EStartTrackingSessionError.GroupNotFound);
                var existing = await trackingSessionRepository.FindActiveByGroupIdAsync(command.ExpeditionGroupId,
                    cancellationToken);
                if (existing is not null && existing.AgencyId == actor.AgencyId)
                    return Success(existing, false);

                // Existing MySQL datetime columns store seconds; retries must return exactly the original time.
                var startedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var preparation = await expeditionSetupContextFacade.StageExpeditionStart(actor,
                    command.ExpeditionGroupId, startedAt, cancellationToken);
                if (preparation != ExpeditionStartStatus.Ready) return Failure(MapError(preparation));

                var session = new TrackingSession(command, startedAt);
                await trackingSessionRepository.AddAsync(session, cancellationToken);
                // Both repositories share AppDbContext. EF commits the group and session atomically.
                await unitOfWork.CompleteAsync(cancellationToken);
                return Success(session, true);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogInformation(ex, "Expedition changed during tracking start; refreshing state.");
                unitOfWork.DiscardChanges();
            }
            catch (Exception ex) when (ex is DbUpdateException update && update.IsDuplicateKeyViolation() ||
                                       ex.IsMySqlTransactionConflict())
            {
                logger.LogInformation(ex, "Concurrent tracking start; refreshing state.");
                unitOfWork.DiscardChanges();
            }
            catch (ArgumentException ex)
            {
                unitOfWork.DiscardChanges();
                logger.LogWarning(ex, "Invalid data starting tracking session.");
                return Failure(EStartTrackingSessionError.InvalidData);
            }
            catch (OperationCanceledException)
            {
                unitOfWork.DiscardChanges();
                throw;
            }
            catch (Exception ex)
            {
                unitOfWork.DiscardChanges();
                logger.LogError(ex, "Unexpected error starting tracking session.");
                return Failure(EStartTrackingSessionError.UnexpectedError);
            }
        }
        return Failure(EStartTrackingSessionError.ConcurrentChange);
    }

    private static Result<TrackingSessionStartResult, EStartTrackingSessionError> Success(TrackingSession session,
        bool created) => new Result<TrackingSessionStartResult, EStartTrackingSessionError>.Success(new(session, created));

    private static Result<TrackingSessionStartResult, EStartTrackingSessionError> Failure(EStartTrackingSessionError error) =>
        new Result<TrackingSessionStartResult, EStartTrackingSessionError>.Failure(error);

    private static EStartTrackingSessionError MapError(ExpeditionStartStatus status) => status switch
    {
        ExpeditionStartStatus.GroupNotFound => EStartTrackingSessionError.GroupNotFound,
        ExpeditionStartStatus.GroupNotScheduled => EStartTrackingSessionError.GroupNotScheduled,
        ExpeditionStartStatus.RouteNotReady => EStartTrackingSessionError.RouteNotReady,
        ExpeditionStartStatus.ProtocolNotReady => EStartTrackingSessionError.ProtocolNotReady,
        ExpeditionStartStatus.ParticipantsNotReady => EStartTrackingSessionError.ParticipantsNotReady,
        _ => EStartTrackingSessionError.UnexpectedError
    };
}
