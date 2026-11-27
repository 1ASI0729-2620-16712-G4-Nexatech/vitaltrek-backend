namespace VitalTrek.API.FieldTracking.Interfaces.ACL.Resources;

/// <summary>
///     Identifiers of an active tracking session, exposed for internal periodic work such as the safety evaluation.
///     It contains no telemetry, participant or progress data.
/// </summary>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="AgencyId">The agency that owns the session.</param>
/// <param name="ExpeditionGroupId">The expedition group tracked by the session.</param>
public sealed record ActiveTrackingSession(int SessionId, int AgencyId, int ExpeditionGroupId);
