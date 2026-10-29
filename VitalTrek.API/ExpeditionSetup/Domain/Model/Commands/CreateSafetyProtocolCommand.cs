namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to configure the safety protocol of a route.
/// </summary>
public record CreateSafetyProtocolCommand(int AgencyId, int RouteId, string Instructions);
