using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;

public static class TrackingProgressResourceAssembler
{
    public static TrackingProgressResource ToResource(TrackingProgress progress) => new(progress.SessionId,
        progress.ExpeditionGroupId, progress.RouteId, progress.Status, progress.StartedAt, progress.EndedAt,
        progress.TotalCheckpoints, progress.ConfirmedCheckpointsCount, progress.EstimatedProgressPercentage,
        progress.LastConfirmedCheckpoint is null ? null : Checkpoint(progress.LastConfirmedCheckpoint),
        progress.NextCheckpoint is null ? null : Checkpoint(progress.NextCheckpoint),
        progress.ConfirmedCheckpoints.Select(Checkpoint).ToArray(),
        progress.Participants.Select(member => new ParticipantTrackingProgressResource(member.MemberId,
            member.WearableDeviceId, member.ClientRecordId, member.Latitude, member.Longitude, member.Altitude,
            member.HeartRate, member.Battery, member.DeviationMeters, member.IsDeviationSimulated, member.RecordedAt,
            member.SynchronizedAt)).ToArray(), progress.SynchronizedAt);

    private static ProgressCheckpointResource Checkpoint(ProgressCheckpoint checkpoint) => new(checkpoint.CheckpointId,
        checkpoint.SequenceOrder, checkpoint.Location, checkpoint.PassedAt, checkpoint.SynchronizedAt);
}
