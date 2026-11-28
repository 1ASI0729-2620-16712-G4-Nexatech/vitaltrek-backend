namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;

/// <summary>
///     Command issued by an adventure tourist to save or clear the optional health information of its own preparation.
/// </summary>
public record RegisterHealthInformationCommand(int AgencyId, int ParticipantPreparationId, string? HealthNotes,
    string RequesterIdentityDocument);
