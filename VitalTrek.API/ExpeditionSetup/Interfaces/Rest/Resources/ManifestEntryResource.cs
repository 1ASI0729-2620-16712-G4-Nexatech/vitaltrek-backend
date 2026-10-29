namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a participant of a tourist manifest.
/// </summary>
public record ManifestEntryResource(
    int Id,
    int AgencyId,
    int ExpeditionGroupId,
    string FullName,
    string IdentityDocument);
