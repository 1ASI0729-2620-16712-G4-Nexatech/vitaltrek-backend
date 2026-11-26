using VitalTrek.API.CheckpointSync.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that validates tracking session information through the Field Tracking facade.
///     The route of a session is resolved through its expedition group using the Expedition Setup facade.
/// </summary>
/// <param name="fieldTrackingContextFacade">The Field Tracking facade.</param>
/// <param name="expeditionSetupContextFacade">The Expedition Setup facade.</param>
public class ExternalFieldTrackingService(
    IFieldTrackingContextFacade fieldTrackingContextFacade,
    IExpeditionSetupContextFacade expeditionSetupContextFacade)
{
    /// <summary>
    ///     Resolves a session within the actor's scope: it must belong to the actor's agency and its expedition group
    ///     must be visible to the actor (for a field guide, a group the guide is assigned to).
    /// </summary>
    /// <returns>The session scope, or <see langword="null"/> when the session is outside the actor's scope.</returns>
    public async Task<SyncSessionScope?> ResolveSessionScope(AuthenticatedActor actor, int sessionId,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0) return null;
        var startedAt = await fieldTrackingContextFacade.FindSessionStartedAtAsync(actor.AgencyId, sessionId,
            cancellationToken);
        if (startedAt is null) return null;
        var groupId = await fieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync(sessionId, cancellationToken);
        if (groupId is null) return null;
        var context = await expeditionSetupContextFacade.FindTrackingProgressContext(actor, groupId.Value,
            cancellationToken);
        if (context is null) return null;
        var isActive = await fieldTrackingContextFacade.IsSessionActiveAsync(sessionId, cancellationToken);
        return new SyncSessionScope(sessionId, groupId.Value, isActive, startedAt.Value,
            context.Checkpoints.OrderBy(checkpoint => checkpoint.SequenceOrder).ToArray());
    }

    /// <summary>
    ///     Stages one reading through the telemetry insertion port agreed for M03. The port never saves:
    ///     the caller owns the transaction.
    /// </summary>
    public Task<TelemetryReadingStageResult> StageReading(AuthenticatedActor actor, int sessionId,
        SyncBatchReadingEntry reading, CancellationToken cancellationToken) =>
        fieldTrackingContextFacade.StageTelemetryReadingAsync(actor, sessionId, new TelemetryReadingInput(
            reading.ClientRecordId, reading.WearableDeviceId, reading.RecordedAt, reading.Latitude, reading.Longitude,
            reading.Altitude, reading.HeartRate, reading.Battery, reading.DeviationMeters), cancellationToken);
}

/// <summary>
///     Session data visible to the actor that a sync batch needs: its state and its ordered route checkpoints.
/// </summary>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="ExpeditionGroupId">The expedition group tracked by the session.</param>
/// <param name="IsActive">Whether the session accepts new captures.</param>
/// <param name="StartedAt">The moment the session started.</param>
/// <param name="RouteCheckpoints">The checkpoints of the session route, ordered by sequence.</param>
public sealed record SyncSessionScope(int SessionId, int ExpeditionGroupId, bool IsActive, DateTimeOffset StartedAt,
    IReadOnlyList<TrackingRouteCheckpoint> RouteCheckpoints)
{
    /// <summary>Returns whether a checkpoint belongs to the session route.</summary>
    public bool ContainsCheckpoint(int checkpointId) => PositionOf(checkpointId) >= 0;

    /// <summary>Returns the zero-based position of a checkpoint in the route, or -1 when it is not on the route.</summary>
    public int PositionOf(int checkpointId)
    {
        for (var index = 0; index < RouteCheckpoints.Count; index++)
            if (RouteCheckpoints[index].Id == checkpointId) return index;
        return -1;
    }

    /// <summary>Returns the checkpoint that must be passed right before another one, or null for the first one.</summary>
    public int? PreviousCheckpointId(int checkpointId)
    {
        var position = PositionOf(checkpointId);
        return position > 0 ? RouteCheckpoints[position - 1].Id : null;
    }
}
