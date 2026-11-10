using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application command service interface for tracking session write operations.
/// </summary>
public interface ITrackingSessionCommandService
{
    /// <summary>Starts a new tracking session for an expedition group.</summary>
    Task<Result<TrackingSession, EStartTrackingSessionError>> Handle(StartTrackingSessionCommand command, CancellationToken cancellationToken);

    /// <summary>Closes an active tracking session.</summary>
    Task<Result<TrackingSession, EEndTrackingSessionError>> Handle(EndTrackingSessionCommand command, CancellationToken cancellationToken);
}
