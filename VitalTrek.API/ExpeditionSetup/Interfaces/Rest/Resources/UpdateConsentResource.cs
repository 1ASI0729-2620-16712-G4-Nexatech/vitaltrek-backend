namespace VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Resources;

/// <summary>
///     Resource to grant (true) or revoke (false) the monitoring consent of a participant.
/// </summary>
public record UpdateConsentResource(
    bool Granted);
