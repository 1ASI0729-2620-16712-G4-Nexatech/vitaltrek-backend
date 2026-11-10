namespace VitalTrek.API.FieldTracking.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a single tracking session by its identifier.
/// </summary>
public record GetTrackingSessionByIdQuery(int AgencyId, int SessionId);
