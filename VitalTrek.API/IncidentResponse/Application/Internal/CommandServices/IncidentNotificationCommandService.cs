using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.IncidentResponse.Domain.Services;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.IncidentResponse.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IIncidentNotificationCommandService"/>. The notification
///     intention, with its identity and stable payload, is committed before the provider is contacted; each attempt is
///     also committed before the call, so two requests never contact the provider for the same notification at once.
/// </summary>
/// <param name="notificationRepository">The incident notification repository.</param>
/// <param name="incidentRepository">The incident repository.</param>
/// <param name="externalIamService">The outbound service that validates recipients.</param>
/// <param name="sender">The provider port.</param>
/// <param name="settings">The notification policy.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class IncidentNotificationCommandService(
    IIncidentNotificationRepository notificationRepository,
    IIncidentRepository incidentRepository,
    ExternalIamService externalIamService,
    IIncidentNotificationSender sender,
    IOptions<IncidentNotificationSettings> settings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<IncidentNotificationCommandService> logger) : IIncidentNotificationCommandService
{
    private IncidentNotificationSettings Policy => settings.Value;

    /// <inheritdoc />
    public async Task<Result<NotificationRequestResult, ESendNotificationError>> Handle(SendNotificationCommand command,
        AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            if (actor.RoleCode != IncidentActorRoles.Administrator) return Fail(ESendNotificationError.AccessForbidden);
            if (actor.AgencyId != command.AgencyId) return Fail(ESendNotificationError.IncidentNotFound);
            if (command.NotificationId == Guid.Empty || command.RecipientType != NotificationRecipientType.AgencyStaff)
                return Fail(ESendNotificationError.InvalidData);
            var address = IncidentNotification.NormalizeAddress(command.RecipientAddress);

            var incident = await incidentRepository.FindByIdAndAgencyIdAsync(command.IncidentId, command.AgencyId,
                cancellationToken);
            if (incident is null) return Fail(ESendNotificationError.IncidentNotFound);

            var fingerprint = IncidentContentFingerprint.ForNotification(command.IncidentId, command.RecipientType, address);
            var existing = await notificationRepository.FindByNotificationIdAsync(command.AgencyId,
                command.NotificationId, cancellationToken);
            if (existing is not null) return await ReuseAsync(existing, command.IncidentId, fingerprint, cancellationToken);

            if (incident.Status != IncidentStatus.EmergencyDeclared)
                return Fail(ESendNotificationError.EmergencyNotDeclared);
            if (!Policy.Allows(address) ||
                !await externalIamService.IsAgencyStaffEmail(command.AgencyId, address, cancellationToken))
                return Fail(ESendNotificationError.RecipientNotAllowed);

            var (subject, body) = IncidentNotificationContent.Build(incident);
            var notification = new IncidentNotification(command with { RecipientAddress = address }, actor.UserId,
                subject, body, fingerprint, timeProvider.GetUtcNow());
            await notificationRepository.AddAsync(notification, cancellationToken);
            try
            {
                // The intention is persisted as pending before any provider is contacted.
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
            {
                logger.LogInformation(ex, "Notification {NotificationId} was requested concurrently", command.NotificationId);
                var committed = await notificationRepository.FindByNotificationIdAsync(command.AgencyId,
                    command.NotificationId, cancellationToken);
                if (committed is null) throw;
                return committed.IncidentId == command.IncidentId && committed.PayloadFingerprint == fingerprint
                    ? Success(committed, false)
                    : Fail(ESendNotificationError.IdentityConflict);
            }

            await AttemptAsync(notification, cancellationToken);
            return Success(notification, true);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid notification request {NotificationId}", command.NotificationId);
            return Fail(ESendNotificationError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error requesting notification {NotificationId}", command.NotificationId);
            return Fail(ESendNotificationError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IncidentNotification, ESendNotificationError>> Handle(
        RetryIncidentNotificationCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            var (notification, error) = await FindAsync(command.AgencyId, command.IncidentId, command.NotificationId,
                actor, cancellationToken);
            if (error is not null) return FailSingle(error.Value);
            if (notification!.Status != NotificationStatus.Pending) return FailSingle(ESendNotificationError.NotRetriable);
            if (notification.HasAttemptInProgress(timeProvider.GetUtcNow(), Policy.AttemptLease))
                return FailSingle(ESendNotificationError.AttemptInProgress);
            return await AttemptAsync(notification, cancellationToken)
                ? new Result<IncidentNotification, ESendNotificationError>.Success(notification)
                : FailSingle(ESendNotificationError.AttemptInProgress);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrying notification {NotificationId}", command.NotificationId);
            return FailSingle(ESendNotificationError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IncidentNotification, ESendNotificationError>> Handle(
        RefreshIncidentNotificationStatusCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            var (notification, error) = await FindAsync(command.AgencyId, command.IncidentId, command.NotificationId,
                actor, cancellationToken);
            if (error is not null) return FailSingle(error.Value);
            if (notification!.Status != NotificationStatus.Accepted || notification.ProviderMessageId is null)
                return new Result<IncidentNotification, ESendNotificationError>.Success(notification);

            IncidentNotificationDeliveryStatus status;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(Policy.AttemptTimeout);
                try
                {
                    status = await sender.GetDeliveryStatusAsync(notification.ProviderMessageId, timeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    status = new IncidentNotificationDeliveryStatus(IncidentNotificationDeliveryOutcome.Unknown,
                        "provider_timeout");
                }
            }

            var now = timeProvider.GetUtcNow();
            switch (status.Outcome)
            {
                case IncidentNotificationDeliveryOutcome.Delivered:
                    notification.MarkDelivered(status.OccurredAt ?? now);
                    break;
                case IncidentNotificationDeliveryOutcome.Failed:
                    notification.MarkFailed(status.Code ?? "provider_reported_failure", status.OccurredAt ?? now);
                    break;
                default:
                    return new Result<IncidentNotification, ESendNotificationError>.Success(notification);
            }

            notificationRepository.Update(notification);
            await unitOfWork.CompleteAsync(CancellationToken.None);
            return new Result<IncidentNotification, ESendNotificationError>.Success(notification);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error refreshing notification {NotificationId}", command.NotificationId);
            return FailSingle(ESendNotificationError.UnexpectedError);
        }
    }

    private async Task<Result<NotificationRequestResult, ESendNotificationError>> ReuseAsync(
        IncidentNotification existing, int incidentId, string fingerprint, CancellationToken cancellationToken)
    {
        if (existing.IncidentId != incidentId || existing.PayloadFingerprint != fingerprint)
            return Fail(ESendNotificationError.IdentityConflict);
        // A repeated request after a lost response retries the same identity, only while it is still pending.
        if (existing.CanStartAttempt(timeProvider.GetUtcNow(), Policy.AttemptLease))
            await AttemptAsync(existing, cancellationToken);
        return Success(existing, false);
    }

    private async Task<(IncidentNotification? Notification, ESendNotificationError? Error)> FindAsync(int agencyId,
        int incidentId, Guid notificationId, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        if (actor.RoleCode != IncidentActorRoles.Administrator) return (null, ESendNotificationError.AccessForbidden);
        if (actor.AgencyId != agencyId) return (null, ESendNotificationError.NotificationNotFound);
        var notification = await notificationRepository.FindByNotificationIdAsync(agencyId, notificationId,
            cancellationToken);
        return notification is null || notification.IncidentId != incidentId
            ? (null, ESendNotificationError.NotificationNotFound)
            : (notification, null);
    }

    /// <summary>
    ///     Runs one provider attempt. The start of the attempt is committed first; a concurrent attempt of the same
    ///     notification fails that commit and does not contact the provider. Returns false in that case.
    /// </summary>
    private async Task<bool> AttemptAsync(IncidentNotification notification, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        if (!notification.CanStartAttempt(startedAt, Policy.AttemptLease)) return false;
        notification.StartAttempt(startedAt, Policy.AttemptLease);
        notificationRepository.Update(notification);
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(ex, "Another attempt of notification {NotificationId} started first",
                notification.NotificationId);
            return false;
        }

        var message = new IncidentNotificationMessage(notification.NotificationId,
            IncidentNotificationMessage.IdempotencyKeyFor(notification.NotificationId), notification.RecipientAddress!,
            notification.Subject!, notification.Body!);
        IncidentNotificationSendResult result;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(Policy.AttemptTimeout);
            try
            {
                result = await sender.SendAsync(message, timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                result = IncidentNotificationSendResult.Uncertain("provider_timeout");
            }
            catch (OperationCanceledException)
            {
                // The client went away; the outcome is unknown and the notification stays pending.
                result = IncidentNotificationSendResult.Uncertain("request_cancelled");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Provider error sending notification {NotificationId}", notification.NotificationId);
                result = IncidentNotificationSendResult.Uncertain("provider_error");
            }
        }

        var finishedAt = timeProvider.GetUtcNow();
        switch (result.Outcome)
        {
            case IncidentNotificationSendOutcome.Accepted when !string.IsNullOrWhiteSpace(result.ProviderMessageId):
                notification.MarkAccepted(result.ProviderMessageId, finishedAt);
                break;
            case IncidentNotificationSendOutcome.Rejected:
                notification.MarkFailed(result.Code ?? "provider_rejected", finishedAt);
                break;
            case IncidentNotificationSendOutcome.NotConfigured:
                notification.MarkAttemptUncertain(result.Code ?? "provider_not_configured", finishedAt);
                break;
            case IncidentNotificationSendOutcome.Accepted:
                notification.MarkAttemptUncertain("provider_missing_message_id", finishedAt);
                break;
            default:
                notification.MarkAttemptUncertain(result.Code ?? "provider_uncertain", finishedAt);
                break;
        }

        notificationRepository.Update(notification);
        // The provider outcome is stored even when the client already disconnected.
        await unitOfWork.CompleteAsync(CancellationToken.None);
        logger.LogInformation("Notification {NotificationId} attempt {Attempt} finished as {Status}",
            notification.NotificationId, notification.AttemptCount, notification.Status.ToCode());
        return true;
    }

    private static Result<NotificationRequestResult, ESendNotificationError> Success(IncidentNotification notification,
        bool created) =>
        new Result<NotificationRequestResult, ESendNotificationError>.Success(
            new NotificationRequestResult(notification, created));

    private static Result<NotificationRequestResult, ESendNotificationError> Fail(ESendNotificationError error) =>
        new Result<NotificationRequestResult, ESendNotificationError>.Failure(error);

    private static Result<IncidentNotification, ESendNotificationError> FailSingle(ESendNotificationError error) =>
        new Result<IncidentNotification, ESendNotificationError>.Failure(error);
}
