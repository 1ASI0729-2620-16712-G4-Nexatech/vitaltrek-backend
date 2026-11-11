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

    /// <summary>
    ///     Checks whether a checkpoint belongs to the route assigned to an expedition group of an agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="expeditionGroupId">The expedition group identifier.</param>
    /// <param name="checkpointId">The checkpoint identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the group exists and the checkpoint is part of its route.</returns>
    Task<bool> ExistsCheckpointInExpeditionGroupRoute(int agencyId, int expeditionGroupId, int checkpointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the expected time windows of the route assigned to an expedition group of an agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="expeditionGroupId">The expedition group identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     The segments of the route with their maximum expected minutes, or an empty list when the group does not exist.
    /// </returns>
    Task<IReadOnlyList<(int FromCheckpointId, int ToCheckpointId, int MaximumMinutes)>> FindExpectedTimeWindowsByExpeditionGroup(
        int agencyId, int expeditionGroupId, CancellationToken cancellationToken = default);
}
