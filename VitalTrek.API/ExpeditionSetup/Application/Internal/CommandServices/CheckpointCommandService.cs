using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="ICheckpointCommandService"/>.
/// </summary>
/// <param name="checkpointRepository">The checkpoint repository.</param>
/// <param name="routeRepository">The route repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class CheckpointCommandService(
    ICheckpointRepository checkpointRepository,
    IRouteRepository routeRepository,
    IUnitOfWork unitOfWork,
    ILogger<CheckpointCommandService> logger) : ICheckpointCommandService
{
    /// <inheritdoc />
    public async Task<Result<Checkpoint, ECreateCheckpointError>> Handle(CreateCheckpointCommand command,
        CancellationToken cancellationToken)
    {
        if (await routeRepository.FindByIdAndAgencyIdAsync(command.RouteId, command.AgencyId, cancellationToken) is null)
            return new Result<Checkpoint, ECreateCheckpointError>.Failure(ECreateCheckpointError.RouteNotFound);
        if (await checkpointRepository.ExistsByRouteIdAndSequenceOrderAsync(command.RouteId, command.SequenceOrder, cancellationToken))
            return new Result<Checkpoint, ECreateCheckpointError>.Failure(ECreateCheckpointError.DuplicateSequenceOrder);
        if (!string.IsNullOrWhiteSpace(command.BluetoothId) &&
            await checkpointRepository.ExistsByBluetoothIdAsync(command.BluetoothId, cancellationToken))
            return new Result<Checkpoint, ECreateCheckpointError>.Failure(ECreateCheckpointError.DuplicateBluetoothId);

        try
        {
            var checkpoint = new Checkpoint(command);
            await checkpointRepository.AddAsync(checkpoint, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Checkpoint, ECreateCheckpointError>.Success(checkpoint);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data creating checkpoint for route {RouteId}", command.RouteId);
            return new Result<Checkpoint, ECreateCheckpointError>.Failure(ECreateCheckpointError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            logger.LogWarning(ex, "Duplicate checkpoint key for route {RouteId}", command.RouteId);
            return new Result<Checkpoint, ECreateCheckpointError>.Failure(ECreateCheckpointError.DuplicateSequenceOrder);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating checkpoint for route {RouteId}", command.RouteId);
            return new Result<Checkpoint, ECreateCheckpointError>.Failure(ECreateCheckpointError.UnexpectedError);
        }
    }
}
