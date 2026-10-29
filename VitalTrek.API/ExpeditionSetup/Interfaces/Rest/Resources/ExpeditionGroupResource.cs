namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing an expedition group. DepartureDate uses the yyyy-MM-dd format.
/// </summary>
public record ExpeditionGroupResource(
    int Id,
    int AgencyId,
    int RouteId,
    string Name,
    DateOnly DepartureDate,
    int MaximumCapacity,
    int? FieldGuideId,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt);
