namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a field guide profile. Code is the operational identifier that tells apart guides with the
///     same name.
/// </summary>
public record FieldGuideResource(
    int Id,
    int AgencyId,
    string Name,
    string Code);
