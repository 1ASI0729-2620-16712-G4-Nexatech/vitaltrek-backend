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
    /// <summary>Checks whether a tracking session exists in an agency.</summary>
    public async Task<bool> ExistsSession(int agencyId, int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.SessionExistsInAgencyAsync(agencyId, sessionId, cancellationToken);

    /// <summary>Checks whether a tracking session is active.</summary>
    public async Task<bool> IsSessionActive(int sessionId, CancellationToken cancellationToken) =>
        await fieldTrackingContextFacade.IsSessionActiveAsync(sessionId, cancellationToken);

    /// <summary>Checks whether a checkpoint belongs to the route of the expedition group tracked by a session.</summary>
    public async Task<bool> ExistsCheckpointInSessionRoute(int agencyId, int sessionId, int checkpointId,
        CancellationToken cancellationToken)
    {
        var groupId = await fieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync(sessionId, cancellationToken);
        return groupId is not null && await expeditionSetupContextFacade.ExistsCheckpointInExpeditionGroupRoute(
            agencyId, groupId.Value, checkpointId, cancellationToken);
    }

    /// <summary>
    ///     Resolves a session within the actor's scope: it must belong to the actor's agency and its expedition group
    ///     must be visible to the actor (for a field guide, a group the guide is assigned to).
    /// </summary>
    /// <returns>The session scope, or <see langword="null"/> when the session is outside the actor's scope.</returns>
    public async Task<SyncSessionScope?> ResolveSessionScope(AuthenticatedActor actor, int sessionId,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0 ||
            !await fieldTrackingContextFacade.SessionExistsInAgencyAsync(actor.AgencyId, sessionId, cancellationToken))
            return null;
        var groupId = await fieldTrackingContextFacade.FindExpeditionGroupIdBySessionIdAsync(sessionId, cancellationToken);
        if (groupId is null) return null;
        var context = await expeditionSetupContextFacade.FindTrackingProgressContext(actor, groupId.Value,
            cancellationToken);
        if (context is null) return null;
        var isActive = await fieldTrackingContextFacade.IsSessionActiveAsync(sessionId, cancellationToken);
        return new SyncSessionScope(sessionId, groupId.Value, isActive, context.Checkpoints);
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
/// <param name="RouteCheckpoints">The checkpoints of the session route, ordered by sequence.</param>
public sealed record SyncSessionScope(int SessionId, int ExpeditionGroupId, bool IsActive,
    IReadOnlyList<TrackingRouteCheckpoint> RouteCheckpoints)
{
    /// <summary>Returns whether a checkpoint belongs to the session route.</summary>
    public bool ContainsCheckpoint(int checkpointId) =>
        RouteCheckpoints.Any(checkpoint => checkpoint.Id == checkpointId);
}
