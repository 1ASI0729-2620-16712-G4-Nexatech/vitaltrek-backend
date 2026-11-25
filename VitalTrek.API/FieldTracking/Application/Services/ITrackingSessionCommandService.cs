using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application command service interface for tracking session write operations.
/// </summary>
public interface ITrackingSessionCommandService
{
    /// <summary>Starts a new tracking session for an expedition group.</summary>
    Task<Result<TrackingSession, EStartTrackingSessionError>> Handle(StartTrackingSessionCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Closes an active tracking session.</summary>
    Task<Result<TrackingSession, EEndTrackingSessionError>> Handle(EndTrackingSessionCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);
}
