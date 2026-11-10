using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;

/// <summary>
///     Assembles Incident Response domain commands from REST resources. The agency always comes from the
///     authenticated user, never from the request body.
/// </summary>
public static class IncidentResponseCommandFromResourceAssembler
{
    /// <summary>
    ///     Transforms a <see cref="DeclareIncidentResource"/> into a <see cref="DeclareIncidentCommand"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a code of the resource is unknown.</exception>
    public static DeclareIncidentCommand ToCommandFromResource(DeclareIncidentResource resource, int agencyId) =>
        new(agencyId, resource.SessionId, IncidentTypeExtensions.FromCode(resource.IncidentType),
            IncidentSeverityExtensions.FromCode(resource.Severity), resource.Description, resource.ReportedAt);

    /// <summary>
    ///     Transforms an <see cref="UpdateIncidentStatusResource"/> into an <see cref="UpdateIncidentStatusCommand"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the status code is unknown.</exception>
    public static UpdateIncidentStatusCommand ToCommandFromResource(UpdateIncidentStatusResource resource,
        int agencyId, int incidentId) =>
        new(agencyId, incidentId, IncidentStatusExtensions.FromCode(resource.Status));

    /// <summary>
    ///     Transforms a <see cref="SendNotificationResource"/> into a <see cref="SendNotificationCommand"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the recipient type code is unknown.</exception>
    public static SendNotificationCommand ToCommandFromResource(SendNotificationResource resource, int agencyId,
        int incidentId) =>
        new(agencyId, incidentId, NotificationRecipientTypeExtensions.FromCode(resource.RecipientType),
            resource.Channel);
}
