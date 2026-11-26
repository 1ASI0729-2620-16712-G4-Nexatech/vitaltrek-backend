using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Application.Internal;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.FieldTracking.Application.ACL;

/// <summary>
///     Implementation of <see cref="IFieldTrackingContextFacade"/> backed by the Field Tracking repositories.
///     The telemetry repository is optional so existing constructions keep compiling; DI always provides it.
/// </summary>
public class FieldTrackingContextFacade(ITrackingSessionRepository trackingSessionRepository,
    IExpeditionSetupContextFacade expeditionSetupContextFacade,
    ITelemetryRecordCommandService telemetryRecordCommandService,
    ITelemetryRecordRepository? telemetryRecordRepository = null)
    : IFieldTrackingContextFacade
{
    /// <inheritdoc />
    public async Task<bool> CanReadSessionProgressAsync(AuthenticatedActor actor, int sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!FieldTrackingAuthorization.CanReadProgress(actor, actor.AgencyId)) return false;
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, actor.AgencyId, cancellationToken);
        return session is not null && await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor,
            session.ExpeditionGroupId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TelemetryReadingStageResult> StageTelemetryReadingAsync(AuthenticatedActor actor, int sessionId,
        TelemetryReadingInput reading, CancellationToken cancellationToken = default)
    {
        var entry = new TelemetryRecordEntry(reading.Latitude, reading.Longitude, reading.Altitude, reading.RecordedAt,
            reading.ClientRecordId, reading.HeartRate, reading.Battery, reading.DeviationMeters);
        var result = await telemetryRecordCommandService.StageReading(new StageTelemetryReadingCommand(actor.AgencyId,
            sessionId, reading.WearableDeviceId, entry), actor, cancellationToken);
        return result switch
        {
            Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Success success => new(reading.ClientRecordId,
                success.Value.IsDuplicate ? TelemetryReadingStageStatus.Duplicate : TelemetryReadingStageStatus.Accepted),
            Result<StagedTelemetryReading, ESubmitTelemetryBatchError>.Failure failure => new(reading.ClientRecordId,
                failure.Error switch
                {
                    ESubmitTelemetryBatchError.AccessForbidden => TelemetryReadingStageStatus.AccessForbidden,
                    ESubmitTelemetryBatchError.InvalidData => TelemetryReadingStageStatus.InvalidData,
                    ESubmitTelemetryBatchError.SessionNotFound => TelemetryReadingStageStatus.SessionNotFound,
                    ESubmitTelemetryBatchError.SessionNotActive => TelemetryReadingStageStatus.SessionNotActive,
                    ESubmitTelemetryBatchError.WearableNotAssigned => TelemetryReadingStageStatus.WearableNotAssigned,
                    ESubmitTelemetryBatchError.ConsentRequired => TelemetryReadingStageStatus.ConsentRequired,
                    ESubmitTelemetryBatchError.RecordIdentityConflict => TelemetryReadingStageStatus.RecordIdentityConflict,
                    ESubmitTelemetryBatchError.ConcurrentChange => TelemetryReadingStageStatus.ConcurrentChange,
                    _ => TelemetryReadingStageStatus.UnexpectedError
                }),
            _ => new(reading.ClientRecordId, TelemetryReadingStageStatus.UnexpectedError)
        };
    }

    /// <inheritdoc />
    public async Task<bool> StageSessionEndAsync(AuthenticatedActor actor, int sessionId, DateTimeOffset endedAt,
        CancellationToken cancellationToken = default)
    {
        if (!FieldTrackingAuthorization.CanWrite(actor, actor.AgencyId)) return false;
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, actor.AgencyId, cancellationToken);
        if (session is null || !await expeditionSetupContextFacade.CanAccessExpeditionGroup(actor,
                session.ExpeditionGroupId, cancellationToken)) return false;
        if (session.Status == SessionStatus.Completed) return true;
        if (!session.IsActive) return false;
        session.End(endedAt);
        return true;
    }

    /// <inheritdoc />
    public async Task<int?> FindActiveSessionIdByGroupIdAsync(int expeditionGroupId, CancellationToken cancellationToken = default)
    {
        var session = await trackingSessionRepository.FindActiveByGroupIdAsync(expeditionGroupId, cancellationToken);
        return session?.Id;
    }

    /// <inheritdoc />
    public async Task<bool> SessionExistsAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await trackingSessionRepository.FindByIdAsync(sessionId, cancellationToken);
        return session is not null;
    }

    /// <inheritdoc />
    public async Task<bool> SessionExistsInAgencyAsync(int agencyId, int sessionId, CancellationToken cancellationToken = default) =>
        await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, agencyId, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<bool> IsSessionActiveAsync(int sessionId, CancellationToken cancellationToken = default) =>
        (await trackingSessionRepository.FindByIdAsync(sessionId, cancellationToken))?.IsActive ?? false;

    /// <inheritdoc />
    public async Task<int?> FindExpeditionGroupIdBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        (await trackingSessionRepository.FindByIdAsync(sessionId, cancellationToken))?.ExpeditionGroupId;

    /// <inheritdoc />
    public async Task<DateTimeOffset?> FindSessionStartedAtAsync(int agencyId, int sessionId,
        CancellationToken cancellationToken = default) =>
        (await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, agencyId, cancellationToken))?.StartedAt;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TelemetryReadingSnapshot>> FindReadingsAsync(int agencyId, int sessionId,
        IReadOnlyCollection<Guid> clientRecordIds, CancellationToken cancellationToken = default)
    {
        if (telemetryRecordRepository is null)
            throw new InvalidOperationException("The telemetry record repository is required to read readings.");
        if (clientRecordIds.Count == 0 ||
            await trackingSessionRepository.FindByIdAndAgencyIdAsync(sessionId, agencyId, cancellationToken) is null)
            return [];
        var records = await telemetryRecordRepository.FindByClientRecordIdsAsync(sessionId, clientRecordIds,
            cancellationToken);
        return records.Select(record => new TelemetryReadingSnapshot(record.ClientRecordId, record.MemberId,
            record.WearableDeviceId, record.RecordedAt, record.SynchronizedAt, record.Latitude, record.Longitude,
            record.Altitude, record.HeartRate, record.Battery, record.DeviationMeters)).ToArray();
    }
}
