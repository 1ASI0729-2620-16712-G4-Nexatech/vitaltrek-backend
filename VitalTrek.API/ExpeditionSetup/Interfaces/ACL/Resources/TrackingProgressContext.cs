namespace VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;

/// <summary>Route and participant identifiers visible to an actor; no personal contact data.</summary>
public sealed record TrackingProgressContext(int RouteId, int? FieldGuideId,
    IReadOnlyList<TrackingRouteCheckpoint> Checkpoints, IReadOnlyCollection<int> MemberIds);

public sealed record TrackingRouteCheckpoint(int Id, int SequenceOrder, string Location);
