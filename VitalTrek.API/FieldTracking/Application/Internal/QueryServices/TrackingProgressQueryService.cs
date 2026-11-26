using VitalTrek.API.CheckpointSync.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Internal.QueryServices;

public sealed class TrackingProgressQueryService(ITrackingSessionRepository sessionRepository,
    ITelemetryRecordRepository telemetryRepository, IExpeditionSetupContextFacade expeditionFacade,
    ICheckpointSyncContextFacade checkpointFacade) : ITrackingProgressQueryService
{
    public async Task<TrackingProgress?> Handle(GetTrackingProgressByGroupIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanReadProgress(actor, query.AgencyId) ||
            !await expeditionFacade.CanAccessExpeditionGroup(actor, query.ExpeditionGroupId, cancellationToken)) return null;
        var session = await sessionRepository.FindLatestByGroupAndAgencyAsync(query.ExpeditionGroupId, actor.AgencyId,
            cancellationToken);
        return session is null ? null : await Handle(new GetTrackingProgressBySessionIdQuery(actor.AgencyId, session.Id),
            actor, cancellationToken);
    }

    public async Task<TrackingProgress?> Handle(GetTrackingProgressBySessionIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        if (!FieldTrackingAuthorization.CanReadProgress(actor, query.AgencyId)) return null;
        var session = await sessionRepository.FindByIdAndAgencyIdAsync(query.SessionId, actor.AgencyId, cancellationToken);
        if (session is null) return null;
        var context = await expeditionFacade.FindTrackingProgressContext(actor, session.ExpeditionGroupId, cancellationToken);
        if (context is null) return null;
        var passages = await checkpointFacade.FindConfirmedPassagesAsync(actor, session.Id, cancellationToken);
        var routeIds = context.Checkpoints.Select(checkpoint => checkpoint.Id).ToHashSet();
        var validPassages = passages.Where(passage => passage.FieldGuideId == context.FieldGuideId &&
            routeIds.Contains(passage.CheckpointId)).ToArray();
        var byCheckpoint = validPassages.GroupBy(passage => passage.CheckpointId)
            .ToDictionary(group => group.Key, group => group.OrderBy(passage => passage.PassedAt).First());
        // A later checkpoint cannot conceal a missing earlier checkpoint.
        var confirmed = context.Checkpoints.TakeWhile(checkpoint => byCheckpoint.ContainsKey(checkpoint.Id))
            .Select(checkpoint => new ProgressCheckpoint(checkpoint.Id, checkpoint.SequenceOrder, checkpoint.Location,
                byCheckpoint[checkpoint.Id].PassedAt, byCheckpoint[checkpoint.Id].SynchronizedAt)).ToArray();
        var next = context.Checkpoints.Skip(confirmed.Length).FirstOrDefault();
        var latest = await telemetryRepository.FindLatestForMembersAsync(session.Id, context.MemberIds, cancellationToken);
        var participants = latest.Select(record => new ParticipantTrackingProgress(record.MemberId!.Value,
            record.WearableDeviceId, record.ClientRecordId, record.Latitude, record.Longitude, record.Altitude,
            record.HeartRate, record.Battery, record.DeviationMeters, record.DeviationMeters.HasValue,
            record.RecordedAt, record.SynchronizedAt)).ToArray();
        var telemetryTime = await telemetryRepository.FindLastSynchronizationAsync(session.Id, cancellationToken);
        var synchronizedAt = validPassages.Select(passage => (DateTimeOffset?)passage.SynchronizedAt)
            .Append(telemetryTime).Max();
        return new TrackingProgress(session.Id, session.ExpeditionGroupId, context.RouteId, session.Status.ToCode(),
            session.StartedAt, session.EndedAt, context.Checkpoints.Count, confirmed.Length,
            context.Checkpoints.Count == 0 ? 0 : (int)Math.Round(100d * confirmed.Length / context.Checkpoints.Count),
            confirmed.LastOrDefault(), next is null ? null : new ProgressCheckpoint(next.Id, next.SequenceOrder, next.Location),
            confirmed, participants, synchronizedAt);
    }
}
