using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Resources;

namespace VitalTrek.API.IncidentResponse.Interfaces.Rest.Transform;

/// <summary>
///     Assembles HTTP action results from the results of the Incident Response command handling. Every error is a
///     problem details response with a localized title and a stable <c>code</c> extension the client translates.
/// </summary>
public static class ActionResultFromIncidentResponseResultAssembler
{
    /// <summary>
    ///     Assembles the action result of a command: 201 with a location header when <paramref name="isCreated"/> returns
    ///     true and <paramref name="createdAtActionName"/> is provided, otherwise 200.
    /// </summary>
    public static ActionResult ToActionResult<TValue, TError, TResource>(
        Result<TValue, TError> result,
        ControllerBase controller,
        IStringLocalizer<SharedResource> localizer,
        Func<TValue, TResource> toResource,
        string? createdAtActionName = null,
        Func<TValue, object>? routeValues = null,
        Func<TValue, bool>? isCreated = null) where TError : Enum =>
        result switch
        {
            Result<TValue, TError>.Success success when createdAtActionName is not null && routeValues is not null &&
                                                        (isCreated?.Invoke(success.Value) ?? true) =>
                controller.CreatedAtAction(createdAtActionName, routeValues(success.Value), toResource(success.Value)),
            Result<TValue, TError>.Success success => controller.Ok(toResource(success.Value)),
            Result<TValue, TError>.Failure failure => ToErrorResult(failure.Error, controller, localizer),
            _ => Problem(controller, localizer, "UnexpectedErrorProcessingRequest", "Incident.UnexpectedError",
                StatusCodes.Status500InternalServerError)
        };

    /// <summary>Builds a coded problem for errors detected in the controller, such as unknown codes.</summary>
    public static ActionResult Problem(ControllerBase controller, IStringLocalizer<SharedResource> localizer,
        string key, string code, int statusCode) =>
        controller.Problem(title: localizer[key].Value, statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static ActionResult ToErrorResult(Enum error, ControllerBase controller,
        IStringLocalizer<SharedResource> localizer)
    {
        var (key, code, status) = error switch
        {
            EDeclareIncidentError.InvalidData => ("InvalidIncidentRequest", "Incident.InvalidData", 400),
            EDeclareIncidentError.AccessForbidden => ("AccessForbidden", "Incident.AccessForbidden", 403),
            EDeclareIncidentError.SessionNotFound => ("TrackingSessionNotFound", "Incident.SessionNotFound", 404),
            EDeclareIncidentError.SessionNotActive => ("TrackingSessionNotActive", "Incident.SessionNotActive", 409),
            EDeclareIncidentError.AlertNotFound => ("EarlyWarningAlertNotFound", "Incident.AlertNotFound", 404),
            EDeclareIncidentError.AlertOutsideSession => ("IncidentAlertOutsideSession", "Incident.AlertOutsideSession", 409),
            EDeclareIncidentError.AlertDismissed => ("IncidentAlertDismissed", "Incident.AlertDismissed", 409),
            EDeclareIncidentError.AlertAlreadyLinked => ("IncidentAlertAlreadyLinked", "Incident.AlertAlreadyLinked", 409),
            EUpdateIncidentStatusError.InvalidData => ("InvalidIncidentStatus", "Incident.InvalidStatus", 400),
            EUpdateIncidentStatusError.AccessForbidden =>
                ("IncidentTransitionForbidden", "Incident.TransitionForbidden", 403),
            EUpdateIncidentStatusError.IncidentNotFound => ("IncidentNotFound", "Incident.NotFound", 404),
            EUpdateIncidentStatusError.InvalidTransition =>
                ("IncidentStatusTransitionNotAllowed", "Incident.InvalidTransition", 409),
            ERecordEvacuationActionError.InvalidData =>
                ("InvalidEvacuationActionRequest", "EvacuationAction.InvalidData", 400),
            ERecordEvacuationActionError.AccessForbidden =>
                ("EvacuationActionForbidden", "EvacuationAction.AccessForbidden", 403),
            ERecordEvacuationActionError.IncidentNotFound => ("IncidentNotFound", "EvacuationAction.IncidentNotFound", 404),
            ERecordEvacuationActionError.EmergencyNotDeclared =>
                ("EvacuationRequiresEmergency", "EvacuationAction.EmergencyNotDeclared", 409),
            ERecordEvacuationActionError.ActionIdentityConflict =>
                ("EvacuationActionIdentityConflict", "EvacuationAction.IdentityConflict", 409),
            ESendNotificationError.InvalidData =>
                ("InvalidIncidentNotificationRequest", "IncidentNotification.InvalidData", 400),
            ESendNotificationError.AccessForbidden =>
                ("IncidentNotificationForbidden", "IncidentNotification.AccessForbidden", 403),
            ESendNotificationError.IncidentNotFound => ("IncidentNotFound", "IncidentNotification.IncidentNotFound", 404),
            ESendNotificationError.EmergencyNotDeclared =>
                ("IncidentNotificationRequiresEmergency", "IncidentNotification.EmergencyNotDeclared", 409),
            ESendNotificationError.RecipientNotAllowed =>
                ("IncidentNotificationRecipientNotAllowed", "IncidentNotification.RecipientNotAllowed", 400),
            ESendNotificationError.NotificationNotFound =>
                ("IncidentNotificationNotFound", "IncidentNotification.NotFound", 404),
            ESendNotificationError.IdentityConflict =>
                ("IncidentNotificationIdentityConflict", "IncidentNotification.IdentityConflict", 409),
            ESendNotificationError.AttemptInProgress =>
                ("IncidentNotificationAttemptInProgress", "IncidentNotification.AttemptInProgress", 409),
            ESendNotificationError.NotRetriable =>
                ("IncidentNotificationNotRetriable", "IncidentNotification.NotRetriable", 409),
            _ => ("UnexpectedErrorProcessingRequest", "Incident.UnexpectedError", 500)
        };
        return Problem(controller, localizer, key, code, status);
    }
}
