using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP response resources from Expedition Setup aggregates.
/// </summary>
public static class ExpeditionSetupResourceAssembler
{
    /// <summary>Transforms a <see cref="Route"/> into a <see cref="RouteResource"/>.</summary>
    public static RouteResource ToResourceFromEntity(Route entity) =>
        new(entity.Id, entity.AgencyId, entity.Name, entity.Origin, entity.Destination, entity.DistanceKm,
            entity.ElevationGainMeters, entity.Difficulty.ToString(), entity.EstimatedDurationMinutes, entity.Status.ToCode());

    /// <summary>Transforms a <see cref="Checkpoint"/> into a <see cref="CheckpointResource"/>.</summary>
    public static CheckpointResource ToResourceFromEntity(Checkpoint entity) =>
        new(entity.Id, entity.AgencyId, entity.RouteId, entity.Location, entity.SequenceOrder, entity.BluetoothId,
            entity.Latitude, entity.Longitude);

    /// <summary>Transforms an <see cref="ExpectedTimeWindow"/> into an <see cref="ExpectedTimeWindowResource"/>.</summary>
    public static ExpectedTimeWindowResource ToResourceFromEntity(ExpectedTimeWindow entity) =>
        new(entity.Id, entity.AgencyId, entity.RouteId, entity.FromCheckpointId, entity.ToCheckpointId,
            entity.MinimumMinutes, entity.MaximumMinutes);

    /// <summary>Transforms a <see cref="RouteNote"/> into a <see cref="RouteNoteResource"/>.</summary>
    public static RouteNoteResource ToResourceFromEntity(RouteNote entity) =>
        new(entity.Id, entity.AgencyId, entity.RouteId, entity.Title, entity.Content, entity.DisplayOrder);

    /// <summary>Transforms a <see cref="SafetyProtocol"/> into a <see cref="SafetyProtocolResource"/>.</summary>
    public static SafetyProtocolResource ToResourceFromEntity(SafetyProtocol entity) =>
        new(entity.Id, entity.AgencyId, entity.RouteId, entity.Instructions, entity.Status.ToCode());

    /// <summary>Transforms a <see cref="FieldGuide"/> into a <see cref="FieldGuideResource"/>.</summary>
    public static FieldGuideResource ToResourceFromEntity(FieldGuide entity) => new(entity.Id, entity.AgencyId, entity.Name);

    /// <summary>Transforms an <see cref="ExpeditionGroup"/> into an <see cref="ExpeditionGroupResource"/>.</summary>
    public static ExpeditionGroupResource ToResourceFromEntity(ExpeditionGroup entity) =>
        new(entity.Id, entity.AgencyId, entity.RouteId, entity.Name, entity.DepartureDate, entity.MaximumCapacity,
            entity.FieldGuideId, entity.Status.ToCode(), entity.StartedAt, entity.FinishedAt);

    /// <summary>Transforms a <see cref="ManifestEntry"/> into a <see cref="ManifestEntryResource"/>.</summary>
    public static ManifestEntryResource ToResourceFromEntity(ManifestEntry entity) =>
        new(entity.Id, entity.AgencyId, entity.ExpeditionGroupId, entity.FullName, entity.IdentityDocument);

    /// <summary>Transforms a <see cref="WearableDevice"/> into a <see cref="WearableDeviceResource"/>.</summary>
    public static WearableDeviceResource ToResourceFromEntity(WearableDevice entity) =>
        new(entity.Id, entity.AgencyId, entity.Code, entity.Status.ToCode());

    /// <summary>Transforms a <see cref="ParticipantPreparation"/> into a <see cref="ParticipantPreparationResource"/>.</summary>
    public static ParticipantPreparationResource ToResourceFromEntity(ParticipantPreparation entity) =>
        new(entity.Id, entity.AgencyId, entity.ExpeditionGroupId, entity.MemberId, entity.WearableId, entity.Consent,
            entity.ConsentRecordedAt, entity.Activated,
            entity.EmergencyContact is { } contact
                ? new EmergencyContactResource(contact.Name, contact.Phone, contact.Relationship)
                : null,
            entity.BaselineMin, entity.BaselineMax, entity.ReturnedAt, entity.ReturnCondition?.ToCode(), entity.IsReady);
}
