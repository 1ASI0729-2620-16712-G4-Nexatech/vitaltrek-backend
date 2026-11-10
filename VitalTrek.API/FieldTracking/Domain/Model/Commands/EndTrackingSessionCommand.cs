namespace VitalTrek.API.FieldTracking.Domain.Model.Commands;

/// <summary>
///     Command to close an active tracking session.
/// </summary>
public record EndTrackingSessionCommand(int AgencyId, int SessionId);
