namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing the preparation of a participant. Ready is true when the wearable is assigned, the emergency contact registered and the consent granted.
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
    bool Ready);
