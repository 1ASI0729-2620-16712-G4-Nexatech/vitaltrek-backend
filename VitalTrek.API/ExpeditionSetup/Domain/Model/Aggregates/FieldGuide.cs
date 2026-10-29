using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the operational profile of an accredited guide of an agency.
/// </summary>
public partial class FieldGuide
{
    /// <summary>Gets the field guide identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency the guide works for.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the full name of the guide.</summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FieldGuide"/> class for Entity Framework Core.
    /// </summary>
    public FieldGuide()
    {
        Name = string.Empty;
    }

    /// <summary>
    ///     Initializes a new field guide profile.
    /// </summary>
    /// <param name="command">The create field guide command.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid.</exception>
    public FieldGuide(CreateFieldGuideCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        Name = DomainGuard.RequireText(command.Name, 120, nameof(command.Name));
    }
}
