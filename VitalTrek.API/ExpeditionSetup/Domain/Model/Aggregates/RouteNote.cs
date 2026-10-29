using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing technical information about the terrain or critical points of a route,
///     uploaded by the agency and consultable offline by guides and tourists.
/// </summary>
public partial class RouteNote
{
    /// <summary>Gets the note identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the note.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the route the note belongs to.</summary>
    public int RouteId { get; private set; }

    /// <summary>Gets the note title.</summary>
    public string Title { get; private set; }

    /// <summary>Gets the note content.</summary>
    public string Content { get; private set; }

    /// <summary>Gets the display position of the note within the route.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RouteNote"/> class for Entity Framework Core.
    /// </summary>
    public RouteNote()
    {
        Title = string.Empty;
        Content = string.Empty;
    }

    /// <summary>
    ///     Initializes a new route note.
    /// </summary>
    /// <param name="command">The create route note command.</param>
    /// <exception cref="ArgumentException">Thrown when the note data is invalid.</exception>
    public RouteNote(CreateRouteNoteCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        RouteId = DomainGuard.RequirePositive(command.RouteId, nameof(command.RouteId));
        Title = DomainGuard.RequireText(command.Title, 120, nameof(command.Title));
        Content = DomainGuard.RequireText(command.Content, 4000, nameof(command.Content));
        DisplayOrder = DomainGuard.RequirePositive(command.DisplayOrder, nameof(command.DisplayOrder));
    }

    /// <summary>
    ///     Updates the note content.
    /// </summary>
    /// <param name="command">The update route note command.</param>
    /// <exception cref="ArgumentException">Thrown when the note data is invalid.</exception>
    public void Update(UpdateRouteNoteCommand command)
    {
        Title = DomainGuard.RequireText(command.Title, 120, nameof(command.Title));
        Content = DomainGuard.RequireText(command.Content, 4000, nameof(command.Content));
        DisplayOrder = DomainGuard.RequirePositive(command.DisplayOrder, nameof(command.DisplayOrder));
    }
}
