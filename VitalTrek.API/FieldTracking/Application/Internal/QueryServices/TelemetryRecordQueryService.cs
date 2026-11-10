using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Queries;
using VitalTrek.API.FieldTracking.Domain.Repositories;

namespace VitalTrek.API.FieldTracking.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="ITelemetryRecordQueryService"/>.
/// </summary>
public class TelemetryRecordQueryService(ITelemetryRecordRepository telemetryRecordRepository)
    : ITelemetryRecordQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<TelemetryRecord>> Handle(GetTelemetryRecordsBySessionIdQuery query, CancellationToken cancellationToken) =>
        await telemetryRecordRepository.FindAllBySessionIdAsync(query.SessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<TelemetryRecord?> Handle(GetLatestTelemetryRecordByWearableIdQuery query, CancellationToken cancellationToken) =>
        await telemetryRecordRepository.FindLatestBySessionIdAndWearableIdAsync(query.SessionId, query.WearableDeviceId, cancellationToken);
}
