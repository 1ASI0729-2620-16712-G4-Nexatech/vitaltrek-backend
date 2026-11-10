using Microsoft.Extensions.Logging;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.FieldTracking.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="ITelemetryRecordCommandService"/>.
/// </summary>
public class TelemetryRecordCommandService(
    ITrackingSessionRepository trackingSessionRepository,
    ITelemetryRecordRepository telemetryRecordRepository,
    IUnitOfWork unitOfWork,
    ILogger<TelemetryRecordCommandService> logger) : ITelemetryRecordCommandService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>> Handle(
        SubmitTelemetryBatchCommand command, CancellationToken cancellationToken)
    {
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(
            command.SessionId, command.AgencyId, cancellationToken);

        if (session is null)
            return new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure(
                ESubmitTelemetryBatchError.SessionNotFound);

        if (!session.IsActive)
            return new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure(
                ESubmitTelemetryBatchError.SessionNotActive);

        try
        {
            var records = command.Records
                .Select(entry => new TelemetryRecord(command.SessionId, command.WearableDeviceId, entry))
                .ToList();

            await telemetryRecordRepository.AddRangeAsync(records, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Success(records);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid telemetry data in batch.");
            return new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure(
                ESubmitTelemetryBatchError.InvalidData);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error submitting telemetry batch.");
            return new Result<IReadOnlyList<TelemetryRecord>, ESubmitTelemetryBatchError>.Failure(
                ESubmitTelemetryBatchError.UnexpectedError);
        }
    }
}
