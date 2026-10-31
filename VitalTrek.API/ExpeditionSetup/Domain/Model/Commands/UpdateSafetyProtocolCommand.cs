using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to update the safety protocol of a route.
/// </summary>
public record UpdateSafetyProtocolCommand(int AgencyId, int SafetyProtocolId, string Instructions, SafetyProtocolStatus Status);
