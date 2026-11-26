using VitalTrek.API.FieldTracking.Application.Model;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.FieldTracking.Application.Services;

public interface ITrackingProgressQueryService
{
    Task<TrackingProgress?> Handle(GetTrackingProgressBySessionIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);
    Task<TrackingProgress?> Handle(GetTrackingProgressByGroupIdQuery query, AuthenticatedActor actor,
        CancellationToken cancellationToken);
}
