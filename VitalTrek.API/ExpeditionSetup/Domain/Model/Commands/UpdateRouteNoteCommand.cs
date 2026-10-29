namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to update a route note.
/// </summary>
public record UpdateRouteNoteCommand(int AgencyId, int RouteNoteId, string Title, string Content, int DisplayOrder);
