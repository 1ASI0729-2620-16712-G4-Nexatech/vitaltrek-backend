namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command issued by an adventure tourist to grant or revoke its monitoring consent, which activates or deactivates its wearable.
/// </summary>
public record UpdateConsentCommand(int AgencyId, int ParticipantPreparationId, bool Granted, string RequesterIdentityDocument);
