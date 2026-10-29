using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;

/// <summary>
///     Assembles Expedition Setup domain commands from REST resources. The agency always comes from the
///     authenticated user, never from the request body.
/// </summary>
public static class ExpeditionSetupCommandFromResourceAssembler
{
    /// <summary>Transforms a <see cref="CreateRouteResource"/> into a <see cref="CreateRouteCommand"/>.</summary>
    public static CreateRouteCommand ToCommandFromResource(CreateRouteResource resource, int agencyId) =>
        new(agencyId, resource.Name, resource.Origin, resource.Destination, resource.DistanceKm,
            resource.ElevationGainMeters, RouteDifficultyExtensions.FromCode(resource.Difficulty),
            resource.EstimatedDurationMinutes);

    /// <summary>Transforms a <see cref="CreateCheckpointResource"/> into a <see cref="CreateCheckpointCommand"/>.</summary>
    public static CreateCheckpointCommand ToCommandFromResource(CreateCheckpointResource resource, int agencyId) =>
        new(agencyId, resource.RouteId, resource.Location, resource.SequenceOrder, resource.BluetoothId,
            resource.Latitude, resource.Longitude);

    /// <summary>Transforms a <see cref="CreateExpectedTimeWindowResource"/> into a <see cref="CreateExpectedTimeWindowCommand"/>.</summary>
    public static CreateExpectedTimeWindowCommand ToCommandFromResource(CreateExpectedTimeWindowResource resource, int agencyId) =>
        new(agencyId, resource.RouteId, resource.FromCheckpointId, resource.ToCheckpointId, resource.MinimumMinutes,
            resource.MaximumMinutes);

    /// <summary>Transforms a <see cref="CreateRouteNoteResource"/> into a <see cref="CreateRouteNoteCommand"/>.</summary>
    public static CreateRouteNoteCommand ToCommandFromResource(CreateRouteNoteResource resource, int agencyId) =>
        new(agencyId, resource.RouteId, resource.Title, resource.Content, resource.DisplayOrder);

    /// <summary>Transforms an <see cref="UpdateRouteNoteResource"/> into an <see cref="UpdateRouteNoteCommand"/>.</summary>
    public static UpdateRouteNoteCommand ToCommandFromResource(UpdateRouteNoteResource resource, int routeNoteId, int agencyId) =>
        new(agencyId, routeNoteId, resource.Title, resource.Content, resource.DisplayOrder);

    /// <summary>Transforms a <see cref="CreateSafetyProtocolResource"/> into a <see cref="CreateSafetyProtocolCommand"/>.</summary>
    public static CreateSafetyProtocolCommand ToCommandFromResource(CreateSafetyProtocolResource resource, int agencyId) =>
        new(agencyId, resource.RouteId, resource.Instructions);

    /// <summary>Transforms an <see cref="UpdateSafetyProtocolResource"/> into an <see cref="UpdateSafetyProtocolCommand"/>.</summary>
    public static UpdateSafetyProtocolCommand ToCommandFromResource(UpdateSafetyProtocolResource resource,
        int safetyProtocolId, int agencyId) =>
        new(agencyId, safetyProtocolId, resource.Instructions, SafetyProtocolStatusExtensions.FromCode(resource.Status));
}
