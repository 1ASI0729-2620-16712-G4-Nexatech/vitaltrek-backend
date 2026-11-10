using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP response resources from Field Tracking aggregates.
/// </summary>
public static class FieldTrackingResourceAssembler
{
    /// <summary>Transforms a <see cref="TrackingSession"/> into a <see cref="TrackingSessionResource"/>.</summary>
    public static TrackingSessionResource ToResourceFromEntity(TrackingSession entity) =>
        new(entity.Id, entity.AgencyId, entity.ExpeditionGroupId, entity.StartedAt, entity.EndedAt,
            entity.Status.ToCode());

    /// <summary>Transforms a <see cref="TelemetryRecord"/> into a <see cref="TelemetryRecordResource"/>.</summary>
    public static TelemetryRecordResource ToResourceFromEntity(TelemetryRecord entity) =>
        new(entity.Id, entity.SessionId, entity.WearableDeviceId, entity.Latitude, entity.Longitude,
            entity.Altitude, entity.RecordedAt, entity.SyncedAt);
}
