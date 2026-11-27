namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing the preparation of a participant. Ready is true when the wearable is assigned, the emergency contact registered and the consent granted.
///     The emergency contact and the optional health notes are returned only inside the scope of the requester: the
///     participant, the agency administrators and the guide assigned to the group.
/// </summary>
public record ParticipantPreparationResource(
    int Id,
    int AgencyId,
    int ExpeditionGroupId,
    int MemberId,
    int? WearableId,
    bool Consent,
    DateTimeOffset? ConsentRecordedAt,
    bool Activated,
    EmergencyContactResource? EmergencyContact,
    int BaselineMin,
    int BaselineMax,
    DateTimeOffset? ReturnedAt,
    string? ReturnCondition,
    bool Ready,
    string? HealthNotes = null,
    DateTimeOffset? HealthNotesRecordedAt = null);
