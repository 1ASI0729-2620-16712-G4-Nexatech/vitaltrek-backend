namespace VitalTrek.API.FieldTracking.Domain.Model.Commands;

/// <summary>
///     Command to start a new GPS tracking session for an expedition group.
/// </summary>
public record StartTrackingSessionCommand(int AgencyId, int ExpeditionGroupId);
