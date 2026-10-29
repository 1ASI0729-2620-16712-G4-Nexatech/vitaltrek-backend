namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Value object describing which expedition data a requester can see inside its agency:
///     administrators see the whole agency, field guides only their groups and adventure tourists only
///     the groups where they appear in the tourist manifest.
/// </summary>
/// <param name="AgencyId">The agency of the requester.</param>
/// <param name="FieldGuideId">The field guide profile of the requester, for field guides.</param>
/// <param name="IdentityDocument">The manifest identity document of the requester, for adventure tourists.</param>
public sealed record AccessScope(int AgencyId, int? FieldGuideId = null, string? IdentityDocument = null)
{
    /// <summary>Gets a value indicating whether the scope covers the whole agency.</summary>
    public bool IsAgencyWide => FieldGuideId is null && IdentityDocument is null;
}
