using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application query service interface for tracking session read operations.
/// </summary>
public interface ITrackingSessionQueryService
{
    /// <summary>Retrieves all tracking sessions for an agency.</summary>
    Task<IEnumerable<TrackingSession>> Handle(GetAllSessionsByAgencyIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Retrieves a tracking session that belongs to an agency.</summary>
    Task<TrackingSession?> Handle(GetTrackingSessionByIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Retrieves the active session for an expedition group.</summary>
    Task<TrackingSession?> Handle(GetActiveSessionByGroupIdQuery query, AuthenticatedActor actor, CancellationToken cancellationToken);
}
