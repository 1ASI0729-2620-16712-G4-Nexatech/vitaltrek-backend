namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register a field guide profile of an agency.
/// </summary>
public record CreateFieldGuideCommand(int AgencyId, string Name);
