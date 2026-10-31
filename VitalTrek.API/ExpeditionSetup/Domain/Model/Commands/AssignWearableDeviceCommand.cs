namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command issued by the responsible field guide to assign a wearable device to a participant.
/// </summary>
public record AssignWearableDeviceCommand(int AgencyId, int ParticipantPreparationId, int WearableDeviceId, int? RequesterFieldGuideId);
