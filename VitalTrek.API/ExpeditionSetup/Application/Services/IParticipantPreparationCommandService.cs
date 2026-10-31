using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application command service interface for participant preparation write operations.</summary>
public interface IParticipantPreparationCommandService
{
    /// <summary>Assigns a wearable device to a participant.</summary>
    Task<Result<ParticipantPreparation, EParticipantPreparationError>> Handle(AssignWearableDeviceCommand command, CancellationToken cancellationToken);

    /// <summary>Registers the emergency contact of a participant.</summary>
    Task<Result<ParticipantPreparation, EParticipantPreparationError>> Handle(RegisterEmergencyContactCommand command, CancellationToken cancellationToken);

    /// <summary>Grants or revokes the monitoring consent of a participant.</summary>
    Task<Result<ParticipantPreparation, EParticipantPreparationError>> Handle(UpdateConsentCommand command, CancellationToken cancellationToken);
}
