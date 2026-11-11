using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Domain.Repositories;

namespace VitalTrek.API.FieldTracking.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ITrackingSessionQueryService"/>.
/// </summary>
public class TrackingSessionQueryService(ITrackingSessionRepository trackingSessionRepository)
    : ITrackingSessionQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<TrackingSession>> Handle(GetAllSessionsByAgencyIdQuery query, CancellationToken cancellationToken) =>
        await trackingSessionRepository.FindAllByAgencyIdAsync(query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<TrackingSession?> Handle(GetTrackingSessionByIdQuery query, CancellationToken cancellationToken) =>
        await trackingSessionRepository.FindByIdAndAgencyIdAsync(query.SessionId, query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<TrackingSession?> Handle(GetActiveSessionByGroupIdQuery query, CancellationToken cancellationToken) =>
        await trackingSessionRepository.FindActiveByGroupIdAsync(query.ExpeditionGroupId, cancellationToken);
}
