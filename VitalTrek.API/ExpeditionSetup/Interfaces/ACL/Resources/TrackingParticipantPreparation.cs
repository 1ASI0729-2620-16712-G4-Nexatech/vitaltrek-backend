namespace VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;

/// <summary>Minimum assignment information needed to authorize telemetry; contains no contact or health details.</summary>
public sealed record TrackingParticipantPreparation(int MemberId, bool Consent, bool Activated, bool WearableAssigned);
