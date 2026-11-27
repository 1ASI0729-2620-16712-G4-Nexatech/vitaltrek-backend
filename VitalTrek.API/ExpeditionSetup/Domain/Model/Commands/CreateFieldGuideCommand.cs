namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register a field guide profile. Code is the optional operational code; the next sequential code of the
///     agency is assigned when it is omitted.
/// </summary>
public record CreateFieldGuideCommand(int AgencyId, string Name, string? Code = null);
