using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;

/// <summary>Application command service that implements <see cref="IParticipantPreparationCommandService"/>.</summary>
/// <param name="participantPreparationRepository">The participant preparation repository.</param>
/// <param name="expeditionGroupRepository">The expedition group repository.</param>
/// <param name="wearableDeviceRepository">The wearable device repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class ParticipantPreparationCommandService(
    IParticipantPreparationRepository participantPreparationRepository,
    IExpeditionGroupRepository expeditionGroupRepository,
    IWearableDeviceRepository wearableDeviceRepository,
    IUnitOfWork unitOfWork,
    ILogger<ParticipantPreparationCommandService> logger) : IParticipantPreparationCommandService
{
    /// <inheritdoc />
    public async Task<Result<ParticipantPreparation, EParticipantPreparationError>> Handle(AssignWearableDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var scope = new AccessScope(command.AgencyId, command.RequesterFieldGuideId);
        var preparation = await participantPreparationRepository.FindByIdAndScopeAsync(command.ParticipantPreparationId,
            scope, cancellationToken);
        if (preparation is null) return Failure(EParticipantPreparationError.ParticipantPreparationNotFound);

        // Wearables are assigned while the group is prepared, before the tour starts.
        var expeditionGroup = await expeditionGroupRepository.FindByIdAndScopeAsync(preparation.ExpeditionGroupId, scope,
            cancellationToken);
        if (expeditionGroup is null || !expeditionGroup.IsScheduled)
            return Failure(EParticipantPreparationError.ExpeditionGroupNotModifiable);

        var wearableDevice = await wearableDeviceRepository.FindByIdAndAgencyIdAsync(command.WearableDeviceId,
            command.AgencyId, cancellationToken);
        if (wearableDevice is null) return Failure(EParticipantPreparationError.WearableDeviceNotFound);
        var keepsSameDevice = preparation.WearableId == wearableDevice.Id && preparation.ReturnedAt is null;
        if (!keepsSameDevice && (!wearableDevice.IsAvailable ||
                                 await participantPreparationRepository.IsWearableInUseAsync(wearableDevice.Id,
                                     preparation.Id, cancellationToken)))
            return Failure(EParticipantPreparationError.WearableDeviceUnavailable);

        var previousWearableId = preparation.ReturnedAt is null ? preparation.WearableId : null;
        return await CommandExecutor.RunAsync(async () =>
            {
                if (!keepsSameDevice)
                {
                    if (previousWearableId is not null)
                    {
                        var previousDevice = await wearableDeviceRepository.FindByIdAndAgencyIdAsync(previousWearableId.Value,
                            command.AgencyId, cancellationToken);
                        previousDevice?.Release();
                    }

                    wearableDevice.MarkAssigned();
                }

                preparation.AssignWearable(wearableDevice.Id);
                participantPreparationRepository.Update(preparation);
                await unitOfWork.CompleteAsync(cancellationToken);
                return preparation;
            },
            ex => ex is InvalidOperationException ? EParticipantPreparationError.WearableDeviceUnavailable : null,
            EParticipantPreparationError.UnexpectedError, logger, "assign wearable device", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ParticipantPreparation, EParticipantPreparationError>> Handle(RegisterEmergencyContactCommand command,
        CancellationToken cancellationToken)
    {
        var (preparation, error) = await FindTouristPreparationAsync(command.AgencyId, command.ParticipantPreparationId,
            command.RequesterIdentityDocument, cancellationToken);
        if (error is not null) return Failure(error.Value);

        return await CommandExecutor.RunAsync(async () =>
            {
                preparation!.RegisterEmergencyContact(new EmergencyContact(command.Name, command.Phone, command.Relationship));
                participantPreparationRepository.Update(preparation);
                await unitOfWork.CompleteAsync(cancellationToken);
                return preparation;
            },
            ex => ex is ArgumentException ? EParticipantPreparationError.InvalidData : null,
            EParticipantPreparationError.UnexpectedError, logger, "register emergency contact", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ParticipantPreparation, EParticipantPreparationError>> Handle(UpdateConsentCommand command,
        CancellationToken cancellationToken)
    {
        var (preparation, error) = await FindTouristPreparationAsync(command.AgencyId, command.ParticipantPreparationId,
            command.RequesterIdentityDocument, cancellationToken);
        if (error is not null) return Failure(error.Value);
        if (command.Granted && preparation!.WearableId is null)
            return Failure(EParticipantPreparationError.WearableDeviceRequired);
        if (command.Granted && preparation!.EmergencyContact is null)
            return Failure(EParticipantPreparationError.EmergencyContactRequired);

        return await CommandExecutor.RunAsync(async () =>
            {
                if (command.Granted) preparation!.GrantConsent(DateTimeOffset.UtcNow);
                else preparation!.RevokeConsent(DateTimeOffset.UtcNow);
                participantPreparationRepository.Update(preparation);
                await unitOfWork.CompleteAsync(cancellationToken);
                return preparation;
            },
            ex => ex is InvalidOperationException ? EParticipantPreparationError.InvalidData : null,
            EParticipantPreparationError.UnexpectedError, logger, "update consent", cancellationToken);
    }

    private async Task<(ParticipantPreparation? Preparation, EParticipantPreparationError? Error)> FindTouristPreparationAsync(
        int agencyId, int participantPreparationId, string requesterIdentityDocument, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requesterIdentityDocument))
            return (null, EParticipantPreparationError.ParticipantPreparationNotFound);
        var scope = new AccessScope(agencyId, IdentityDocument: requesterIdentityDocument);
        var preparation = await participantPreparationRepository.FindByIdAndScopeAsync(participantPreparationId, scope,
            cancellationToken);
        if (preparation is null) return (null, EParticipantPreparationError.ParticipantPreparationNotFound);
        var expeditionGroup = await expeditionGroupRepository.FindByIdAndScopeAsync(preparation.ExpeditionGroupId,
            new AccessScope(agencyId), cancellationToken);
        return expeditionGroup is null || expeditionGroup.Status == ExpeditionGroupStatus.Finished
            ? (null, EParticipantPreparationError.ExpeditionGroupNotModifiable)
            : (preparation, null);
    }

    private static Result<ParticipantPreparation, EParticipantPreparationError> Failure(EParticipantPreparationError error) =>
        new Result<ParticipantPreparation, EParticipantPreparationError>.Failure(error);
}
