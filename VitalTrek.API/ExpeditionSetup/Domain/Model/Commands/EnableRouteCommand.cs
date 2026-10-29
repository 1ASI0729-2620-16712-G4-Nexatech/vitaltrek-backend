namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to enable a route for tour planning.
/// </summary>
/// <param name="AgencyId">The agency that owns the route.</param>
/// <param name="RouteId">The route identifier.</param>
public record EnableRouteCommand(int AgencyId, int RouteId);
