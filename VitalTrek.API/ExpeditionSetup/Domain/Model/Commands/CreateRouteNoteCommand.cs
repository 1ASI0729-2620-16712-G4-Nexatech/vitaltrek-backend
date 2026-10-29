namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command to register a route note uploaded by the agency.
/// </summary>
public record CreateRouteNoteCommand(int AgencyId, int RouteId, string Title, string Content, int DisplayOrder);
