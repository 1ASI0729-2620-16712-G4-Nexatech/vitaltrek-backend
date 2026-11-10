using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application command service interface for telemetry record write operations.
/// </summary>
public interface ITelemetryRecordCommandService
{
    /// <summary>Persists a batch of telemetry records for a wearable device.</summary>
    Task<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>> Handle(SubmitTelemetryBatchCommand command, CancellationToken cancellationToken);
}
