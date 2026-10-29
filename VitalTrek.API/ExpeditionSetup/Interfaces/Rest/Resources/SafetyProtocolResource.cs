namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a safety protocol returned to clients.
/// </summary>
public record SafetyProtocolResource(
    int Id,
    int AgencyId,
    int RouteId,
    string Instructions,
    string Status);
