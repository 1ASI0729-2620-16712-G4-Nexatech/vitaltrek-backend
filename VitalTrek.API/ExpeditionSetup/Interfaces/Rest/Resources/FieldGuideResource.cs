namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a field guide profile.
/// </summary>
public record FieldGuideResource(
    int Id,
    int AgencyId,
    string Name);
