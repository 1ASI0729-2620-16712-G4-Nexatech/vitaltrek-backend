using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.FieldTracking.Application.Model;

namespace VitalTrek.API.FieldTracking.Application.Services;

/// <summary>
///     Application command service interface for telemetry record write operations.
/// </summary>
public interface ITelemetryRecordCommandService
{
    /// <summary>Validates and stages one reading. Never saves; the caller owns the surrounding transaction.</summary>
    Task<Result<StagedTelemetryReading, ESubmitTelemetryBatchError>> StageReading(
        StageTelemetryReadingCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);

    /// <summary>Persists a batch of telemetry records for a wearable device.</summary>
    Task<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>> Handle(SubmitTelemetryBatchCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);
}
