using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.IncidentResponse.Domain.Services;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.IncidentResponse.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IEvacuationActionCommandService"/>. The assigned field guide
///     may capture actions offline; each capture carries a client identity. A retry with the same identity and content
///     returns the first record without modifying it, a reused identity with different content is rejected.
/// </summary>
/// <param name="evacuationActionRepository">The evacuation action repository.</param>
/// <param name="incidentRepository">The incident repository.</param>
/// <param name="externalExpeditionSetupService">The outbound service that resolves the group scope of the actor.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class EvacuationActionCommandService(
    IEvacuationActionRepository evacuationActionRepository,
    IIncidentRepository incidentRepository,
    ExternalExpeditionSetupService externalExpeditionSetupService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<EvacuationActionCommandService> logger) : IEvacuationActionCommandService
{
    /// <inheritdoc />
    public async Task<Result<EvacuationActionRecordResult, ERecordEvacuationActionError>> Handle(
        RecordEvacuationActionCommand command, AuthenticatedActor actor, CancellationToken cancellationToken)
    {
        try
        {
            if (actor.RoleCode != IncidentActorRoles.FieldGuide) return Fail(ERecordEvacuationActionError.AccessForbidden);
            if (actor.AgencyId != command.AgencyId) return Fail(ERecordEvacuationActionError.IncidentNotFound);
            var receivedAt = timeProvider.GetUtcNow();
            if (command.ClientActionId == Guid.Empty || !Enum.IsDefined(command.Status) ||
                command.RecordedAt > receivedAt + Incident.ClockTolerance)
                return Fail(ERecordEvacuationActionError.InvalidData);

            var incident = await incidentRepository.FindByIdAndAgencyIdAsync(command.IncidentId, command.AgencyId,
                cancellationToken);
            if (incident is null ||
                !await externalExpeditionSetupService.CanAccessGroup(actor, incident.ExpeditionGroupId, cancellationToken))
                return Fail(ERecordEvacuationActionError.IncidentNotFound);

            var fingerprint = IncidentContentFingerprint.ForEvacuationAction(command);
            var existing = await evacuationActionRepository.FindByClientActionIdAsync(command.AgencyId,
                command.ClientActionId, cancellationToken);
            if (existing is not null) return Replay(existing, command, fingerprint);

            if (!incident.AcceptsEvacuationCapturedAt(command.RecordedAt))
                return Fail(ERecordEvacuationActionError.EmergencyNotDeclared);

            var action = new EvacuationAction(command, incident.ExpeditionGroupId, actor.UserId, receivedAt, fingerprint);
            await evacuationActionRepository.AddAsync(action, cancellationToken);
            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
            {
                // A concurrent retry of the same capture committed first: answer with that record.
                logger.LogInformation(ex, "Evacuation action {ClientActionId} was recorded concurrently",
                    command.ClientActionId);
                var committed = await evacuationActionRepository.FindByClientActionIdAsync(command.AgencyId,
                    command.ClientActionId, cancellationToken);
                if (committed is null) throw;
                return Replay(committed, command, fingerprint);
            }

            return new Result<EvacuationActionRecordResult, ERecordEvacuationActionError>.Success(
                new EvacuationActionRecordResult(action, true));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid evacuation action {ClientActionId}", command.ClientActionId);
            return Fail(ERecordEvacuationActionError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error recording evacuation action {ClientActionId}", command.ClientActionId);
            return Fail(ERecordEvacuationActionError.UnexpectedError);
        }
    }

    private static Result<EvacuationActionRecordResult, ERecordEvacuationActionError> Replay(EvacuationAction existing,
        RecordEvacuationActionCommand command, string fingerprint) =>
        existing.IncidentId == command.IncidentId && existing.ContentFingerprint == fingerprint
            ? new Result<EvacuationActionRecordResult, ERecordEvacuationActionError>.Success(
                new EvacuationActionRecordResult(existing, false))
            : Fail(ERecordEvacuationActionError.ActionIdentityConflict);

    private static Result<EvacuationActionRecordResult, ERecordEvacuationActionError> Fail(
        ERecordEvacuationActionError error) =>
        new Result<EvacuationActionRecordResult, ERecordEvacuationActionError>.Failure(error);
}
