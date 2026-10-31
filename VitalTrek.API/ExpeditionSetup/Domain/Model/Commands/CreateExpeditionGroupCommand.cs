namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to create an expedition group for an enabled route.
/// </summary>
public record CreateExpeditionGroupCommand(int AgencyId, int RouteId, string Name, DateOnly DepartureDate, int MaximumCapacity);
