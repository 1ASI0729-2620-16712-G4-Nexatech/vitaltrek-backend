using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
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

/// <summary>Direct REST ingestion and transaction-free staging share the same checks and capture identity.</summary>
public class TelemetryRecordCommandService(
    ITrackingSessionRepository trackingSessionRepository,
    ITelemetryRecordRepository telemetryRecordRepository,
    IExpeditionSetupContextFacade expeditionSetupContextFacade,
    IUnitOfWork unitOfWork,
    ILogger<TelemetryRecordCommandService> logger) : ITelemetryRecordCommandService
{
    public async Task<Result<StagedTelemetryReading, ESubmitTelemetryBatchError>> StageReading(
        StageTelemetryReadingCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            var authorization = await AuthorizeSession(command.AgencyId, command.SessionId, actor, cancellationToken);
            if (authorization is Result<TrackingSession, ESubmitTelemetryBatchError>.Failure denied)
                return new Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Failure(denied.Error);
            var session = ((Result<TrackingSession, ESubmitTelemetryBatchError>.Success)authorization).Value;
            if (command.Reading is null || command.WearableDeviceId <= 0)
                return StageFailure(ESubmitTelemetryBatchError.InvalidData);
            var preparation = await expeditionSetupContextFacade.FindTrackingPreparation(actor,
                session.ExpeditionGroupId, command.WearableDeviceId, cancellationToken);
            if (preparation is not { WearableAssigned: true })
                return StageFailure(ESubmitTelemetryBatchError.WearableNotAssigned);
            if (!preparation.Consent || !preparation.Activated)
                return StageFailure(ESubmitTelemetryBatchError.ConsentRequired);

            var record = new TelemetryRecord(session.Id, command.WearableDeviceId, command.Reading, preparation.MemberId);
            if (record.RecordedAt < session.StartedAt.AddMinutes(-1))
                return StageFailure(ESubmitTelemetryBatchError.InvalidData);
            var existing = await telemetryRecordRepository.FindByClientRecordIdAsync(session.Id,
                record.ClientRecordId, cancellationToken);
            if (existing is not null)
                return existing.HasSameCapture(record)
                    ? new Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Success(new(existing, true))
                    : StageFailure(ESubmitTelemetryBatchError.RecordIdentityConflict);
            await telemetryRecordRepository.AddRangeAsync([record], cancellationToken);
            return new Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Success(new(record, false));
        }
        catch (ArgumentException)
        {
            return StageFailure(ESubmitTelemetryBatchError.InvalidData);
        }
    }

    public async Task<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>> Handle(
        SubmitTelemetryBatchCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var authorization = await AuthorizeSession(command.AgencyId, command.SessionId, actor, cancellationToken);
                if (authorization is Result<TrackingSession, ESubmitTelemetryBatchError>.Failure denied)
                    return Failure(denied.Error);
                if (command.Records is null || command.Records.Count is < 1 or > 500)
                    return Failure(ESubmitTelemetryBatchError.InvalidData);
                var records = new List<TelemetryRecord>();
                var hasNewRecords = false;
                foreach (var entry in command.Records)
                {
                    var staged = await StageReading(new StageTelemetryReadingCommand(command.AgencyId,
                        command.SessionId, command.WearableDeviceId, entry), actor, cancellationToken);
                    if (staged is Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Failure failure)
                    {
                        unitOfWork.DiscardChanges();
                        return Failure(failure.Error);
                    }
                    var accepted = ((Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Success)staged).Value;
                    hasNewRecords |= !accepted.IsDuplicate;
                    if (records.All(record => record.ClientRecordId != accepted.Record.ClientRecordId))
                        records.Add(accepted.Record);
                }
                if (hasNewRecords) await unitOfWork.CompleteAsync(cancellationToken);
                return new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Success(records);
            }
            catch (Exception ex) when (ex is DbUpdateException update && update.IsDuplicateKeyViolation() ||
                                       ex.IsMySqlTransactionConflict())
            {
                logger.LogInformation(ex, "Concurrent telemetry ingestion; rechecking capture identities.");
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
                logger.LogError(ex, "Unexpected telemetry ingestion failure.");
                return Failure(ESubmitTelemetryBatchError.UnexpectedError);
            }
        }
        return Failure(ESubmitTelemetryBatchError.ConcurrentChange);
    }

    private async Task<Result<TrackingSession, ESubmitTelemetryBatchError>> AuthorizeSession(int agencyId, int sessionId,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanWrite(actor, agencyId))
            return new Result<TrackingSession, ESubmitTelemetryBatchError>.Failure(ESubmitTelemetryBatchError.AccessForbidden);
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, agencyId, cancellationToken);
        if (session is null || !await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor,
                session.ExpeditionGroupId, cancellationToken))
            return new Result<TrackingSession, ESubmitTelemetryBatchError>.Failure(ESubmitTelemetryBatchError.SessionNotFound);
        return session.IsActive
            ? new Result<TrackingSession, ESubmitTelemetryBatchError>.Success(session)
            : new Result<TrackingSession, ESubmitTelemetryBatchError>.Failure(ESubmitTelemetryBatchError.SessionNotActive);
    }

    private static Result<StagedTelemetryReading, ESubmitTelemetryBatchError> StageFailure(ESubmitTelemetryBatchError error) =>
        new Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Failure(error);

    private static Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError> Failure(ESubmitTelemetryBatchError error) =>
        new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure(error);
}
