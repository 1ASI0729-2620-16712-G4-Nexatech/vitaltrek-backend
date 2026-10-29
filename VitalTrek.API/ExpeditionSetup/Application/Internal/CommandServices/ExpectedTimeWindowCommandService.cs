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
///     Application command service that implements <see cref="IExpectedTimeWindowCommandService"/>.
/// </summary>
/// <param name="expectedTimeWindowRepository">The expected time window repository.</param>
/// <param name="routeRepository">The route repository.</param>
/// <param name="checkpointRepository">The checkpoint repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class ExpectedTimeWindowCommandService(
    IExpectedTimeWindowRepository expectedTimeWindowRepository,
    IRouteRepository routeRepository,
    ICheckpointRepository checkpointRepository,
    IUnitOfWork unitOfWork,
    ILogger<ExpectedTimeWindowCommandService> logger) : IExpectedTimeWindowCommandService
{
    /// <inheritdoc />
    public async Task<Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>> Handle(
        CreateExpectedTimeWindowCommand command, CancellationToken cancellationToken)
    {
        if (await routeRepository.FindByIdAndAgencyIdAsync(command.RouteId, command.AgencyId, cancellationToken) is null)
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Failure(
                ECreateExpectedTimeWindowError.RouteNotFound);

        // A segment is formed by two checkpoints that are consecutive in the route sequence.
        var checkpoints = await checkpointRepository.FindByRouteIdAsync(command.RouteId, cancellationToken);
        var fromIndex = checkpoints.ToList().FindIndex(checkpoint => checkpoint.Id == command.FromCheckpointId);
        if (fromIndex < 0 || fromIndex + 1 >= checkpoints.Count || checkpoints[fromIndex + 1].Id != command.ToCheckpointId)
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Failure(
                ECreateExpectedTimeWindowError.SegmentNotFound);

        if (await expectedTimeWindowRepository.ExistsBySegmentAsync(command.RouteId, command.FromCheckpointId,
                command.ToCheckpointId, cancellationToken))
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Failure(
                ECreateExpectedTimeWindowError.DuplicateSegment);

        try
        {
            var expectedTimeWindow = new ExpectedTimeWindow(command);
            await expectedTimeWindowRepository.AddAsync(expectedTimeWindow, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Success(expectedTimeWindow);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid limits for expected time window of route {RouteId}", command.RouteId);
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Failure(
                ECreateExpectedTimeWindowError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Failure(
                ECreateExpectedTimeWindowError.DuplicateSegment);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating expected time window for route {RouteId}", command.RouteId);
            return new Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>.Failure(
                ECreateExpectedTimeWindowError.UnexpectedError);
        }
    }
}
