using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.IncidentResponse.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IIncidentCommandService"/>. The agency, the expedition
///     group and the role always come from the server: the group is resolved from the session and the actor must see it.
///     Resources outside the scope of the actor are reported as not found.
/// </summary>
/// <param name="incidentRepository">The incident repository.</param>
/// <param name="externalTrackingSessionService">The outbound service that validates the tracking session.</param>
/// <param name="externalExpeditionSetupService">The outbound service that resolves the group scope of the actor.</param>
/// <param name="externalSafetyMonitoringService">The outbound service that reads alert references.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class IncidentCommandService(
    IIncidentRepository incidentRepository,
    ExternalTrackingSessionService externalTrackingSessionService,
    ExternalExpeditionSetupService externalExpeditionSetupService,
    ExternalSafetyMonitoringService externalSafetyMonitoringService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<IncidentCommandService> logger) : IIncidentCommandService
{
    /// <inheritdoc />
    public async Task<Result<Incident, EDeclareIncidentError>> Handle(DeclareIncidentCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            if (!IncidentActorRoles.IsStaff(actor)) return Fail(EDeclareIncidentError.AccessForbidden);
            if (actor.AgencyId != command.AgencyId) return Fail(EDeclareIncidentError.SessionNotFound);

            var groupId = await externalTrackingSessionService.FindExpeditionGroupId(command.AgencyId,
                command.SessionId, cancellationToken);
            if (groupId is null ||
                !await externalExpeditionSetupService.CanAccessGroup(actor, groupId.Value, cancellationToken))
                return Fail(EDeclareIncidentError.SessionNotFound);
            if (!await externalTrackingSessionService.IsSessionActive(command.SessionId, cancellationToken))
                return Fail(EDeclareIncidentError.SessionNotActive);

            if (command.AlertId is { } alertId)
            {
                var alert = await externalSafetyMonitoringService.FindAlert(command.AgencyId, alertId, cancellationToken);
                if (alert is null) return Fail(EDeclareIncidentError.AlertNotFound);
                if (alert.SessionId != command.SessionId || alert.ExpeditionGroupId != groupId.Value)
                    return Fail(EDeclareIncidentError.AlertOutsideSession);
                if (alert.StatusCode == ExternalSafetyMonitoringService.DismissedAlertStatusCode)
                    return Fail(EDeclareIncidentError.AlertDismissed);
                if (await incidentRepository.FindByAlertIdAsync(command.AgencyId, alertId, cancellationToken) is not null)
                    return Fail(EDeclareIncidentError.AlertAlreadyLinked);
            }

            var incident = new Incident(command, groupId.Value, actor.UserId, timeProvider.GetUtcNow());
            await incidentRepository.AddAsync(incident, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Incident, EDeclareIncidentError>.Success(incident);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data reporting incident for session {SessionId}", command.SessionId);
            return Fail(EDeclareIncidentError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation() && command.AlertId is not null)
        {
            // A concurrent report from the same alert committed first; the unique alert index keeps a single incident.
            logger.LogWarning(ex, "Alert {AlertId} was linked concurrently to another incident", command.AlertId);
            return Fail(EDeclareIncidentError.AlertAlreadyLinked);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error reporting incident for session {SessionId}", command.SessionId);
            return Fail(EDeclareIncidentError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Incident, EUpdateIncidentStatusError>> Handle(UpdateIncidentStatusCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            if (!Enum.IsDefined(command.Status)) return Fail(EUpdateIncidentStatusError.InvalidData);
            if (!IncidentActorRoles.IsStaff(actor)) return Fail(EUpdateIncidentStatusError.AccessForbidden);
            if (actor.AgencyId != command.AgencyId) return Fail(EUpdateIncidentStatusError.IncidentNotFound);

            var incident = await incidentRepository.FindByIdAndAgencyIdAsync(command.IncidentId, command.AgencyId,
                cancellationToken);
            if (incident is null ||
                !await externalExpeditionSetupService.CanAccessGroup(actor, incident.ExpeditionGroupId, cancellationToken))
                return Fail(EUpdateIncidentStatusError.IncidentNotFound);
            if (!IncidentActorRoles.CanMoveTo(actor, command.Status))
                return Fail(EUpdateIncidentStatusError.AccessForbidden);
            if (!incident.CanMoveTo(command.Status)) return Fail(EUpdateIncidentStatusError.InvalidTransition);

            var now = timeProvider.GetUtcNow();
            switch (command.Status)
            {
                case IncidentStatus.Confirmed:
                    incident.Confirm(actor.UserId, now, command.Note);
                    break;
                case IncidentStatus.Dismissed:
                    incident.Dismiss(actor.UserId, now, command.Note);
                    break;
                case IncidentStatus.EmergencyDeclared:
                    incident.DeclareEmergency(actor.UserId, now, command.Note);
                    break;
                case IncidentStatus.Resolved:
                    incident.Resolve(actor.UserId, now, command.Note);
                    break;
                case IncidentStatus.Closed:
                    incident.Close(actor.UserId, now, command.Note);
                    break;
                default:
                    return Fail(EUpdateIncidentStatusError.InvalidTransition);
            }

            incidentRepository.Update(incident);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Incident, EUpdateIncidentStatusError>.Success(incident);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid status transition for incident {IncidentId}", command.IncidentId);
            return Fail(EUpdateIncidentStatusError.InvalidTransition);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data updating incident {IncidentId}", command.IncidentId);
            return Fail(EUpdateIncidentStatusError.InvalidData);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another transition of the same incident committed first.
            logger.LogWarning(ex, "Concurrent transition of incident {IncidentId}", command.IncidentId);
            return Fail(EUpdateIncidentStatusError.InvalidTransition);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating incident {IncidentId}", command.IncidentId);
            return Fail(EUpdateIncidentStatusError.UnexpectedError);
        }
    }

    private static Result<Incident, EDeclareIncidentError> Fail(EDeclareIncidentError error) =>
        new Result<Incident, EDeclareIncidentError>.Failure(error);

    private static Result<Incident, EUpdateIncidentStatusError> Fail(EUpdateIncidentStatusError error) =>
        new Result<Incident, EUpdateIncidentStatusError>.Failure(error);
}
