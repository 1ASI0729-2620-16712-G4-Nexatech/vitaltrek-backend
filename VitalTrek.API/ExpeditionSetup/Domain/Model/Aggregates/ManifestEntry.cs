using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing an adventure tourist registered in the tourist manifest of an expedition group.
/// </summary>
public partial class ManifestEntry
{
    /// <summary>Gets the manifest entry identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the manifest.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the expedition group of the manifest.</summary>
    public int ExpeditionGroupId { get; private set; }

    /// <summary>Gets the full name of the tourist.</summary>
    public string FullName { get; private set; }

    /// <summary>Gets the identity document of the tourist.</summary>
    public string IdentityDocument { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ManifestEntry"/> class for Entity Framework Core.
    /// </summary>
    public ManifestEntry()
    {
        FullName = string.Empty;
        IdentityDocument = string.Empty;
    }

    /// <summary>
    ///     Initializes a new manifest entry.
    /// </summary>
    /// <param name="command">The create manifest entry command.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid.</exception>
    public ManifestEntry(CreateManifestEntryCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        ExpeditionGroupId = DomainGuard.RequirePositive(command.ExpeditionGroupId, nameof(command.ExpeditionGroupId));
        FullName = DomainGuard.RequireText(command.FullName, 120, nameof(command.FullName));
        IdentityDocument = DomainGuard.RequireText(command.IdentityDocument, 20, nameof(command.IdentityDocument));
    }
}
