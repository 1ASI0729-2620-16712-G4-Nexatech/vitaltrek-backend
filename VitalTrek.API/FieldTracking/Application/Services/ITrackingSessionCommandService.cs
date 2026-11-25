using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Application.Model;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application command service interface for tracking session write operations.
/// </summary>
public interface ITrackingSessionCommandService
{
    /// <summary>Starts a prepared expedition or returns the existing active session without resetting its time.</summary>
    Task<Result<TrackingSessionStartResult, EStartTrackingSessionError>> Handle(StartTrackingSessionCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);
}
