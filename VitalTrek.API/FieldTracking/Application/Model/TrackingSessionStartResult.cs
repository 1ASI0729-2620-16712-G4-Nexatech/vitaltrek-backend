using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;

namespace VitalTrek.API.FieldTracking.Application.Model;

/// <summary>Distinguishes a newly created session from an idempotent retry.</summary>
public sealed record TrackingSessionStartResult(TrackingSession Session, bool WasCreated);
