using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;

/// <summary>
///     Assembles Field Tracking domain commands from REST resources.
///     The agency identifier always comes from the authenticated user, never from the request body.
/// </summary>
public static class FieldTrackingCommandFromResourceAssembler
{
    /// <summary>Transforms a <see cref="StartTrackingSessionResource"/> into a <see cref="StartTrackingSessionCommand"/>.</summary>
    public static StartTrackingSessionCommand ToCommandFromResource(StartTrackingSessionResource resource, int agencyId) =>
        new(agencyId, resource.ExpeditionGroupId);

    /// <summary>Transforms a <see cref="SubmitTelemetryBatchResource"/> into a <see cref="SubmitTelemetryBatchCommand"/>.</summary>
    public static SubmitTelemetryBatchCommand ToCommandFromResource(SubmitTelemetryBatchResource resource, int agencyId, int sessionId) =>
        new(agencyId, sessionId, resource.WearableDeviceId,
            resource.Records.Select(e => new TelemetryRecordEntry(e.Latitude, e.Longitude, e.Altitude, e.RecordedAt)).ToList());
}
