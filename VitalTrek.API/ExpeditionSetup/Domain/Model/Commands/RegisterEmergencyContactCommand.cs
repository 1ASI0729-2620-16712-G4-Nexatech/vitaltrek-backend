namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command issued by an adventure tourist to register its emergency contact.
/// </summary>
public record RegisterEmergencyContactCommand(int AgencyId, int ParticipantPreparationId, string Name, string Phone, string? Relationship, string RequesterIdentityDocument);
