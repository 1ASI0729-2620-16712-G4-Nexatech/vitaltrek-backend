namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register an adventure tourist in the tourist manifest of a group. When issued by a field guide, the group must be assigned to that guide.
/// </summary>
public record CreateManifestEntryCommand(int AgencyId, int ExpeditionGroupId, string FullName, string IdentityDocument, int? RequesterFieldGuideId);
