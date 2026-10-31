namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a route note returned to clients.
/// </summary>
public record RouteNoteResource(
    int Id,
    int AgencyId,
    int RouteId,
    string Title,
    string Content,
    int DisplayOrder);
