namespace VitalTrek.API.ExpeditionSetup.Interfaces.ACL;

/// <summary>
///     Anti-corruption layer facade that exposes Expedition Setup information to other bounded contexts
///     without sharing the Expedition Setup domain model.
/// </summary>
public interface IExpeditionSetupContextFacade
{
    /// <summary>
    ///     Checks whether a field guide profile exists in an agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="fieldGuideId">The field guide identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the profile exists.</returns>
    Task<bool> ExistsFieldGuide(int agencyId, int fieldGuideId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether an identity document appears in a tourist manifest of an agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="identityDocument">The identity document.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the document is registered.</returns>
    Task<bool> ExistsManifestIdentityDocument(int agencyId, string identityDocument, CancellationToken cancellationToken = default);
}
