using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IRouteCommandService"/>.
/// </summary>
/// <param name="routeRepository">The route repository.</param>
/// <param name="checkpointRepository">The checkpoint repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class RouteCommandService(
    IRouteRepository routeRepository,
    ICheckpointRepository checkpointRepository,
    IUnitOfWork unitOfWork,
    ILogger<RouteCommandService> logger) : IRouteCommandService
{
    /// <inheritdoc />
    public async Task<Result<Route, ECreateRouteError>> Handle(CreateRouteCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Name) &&
            await routeRepository.ExistsByAgencyIdAndNameAsync(command.AgencyId, command.Name, cancellationToken))
            return new Result<Route, ECreateRouteError>.Failure(ECreateRouteError.DuplicateName);

        try
        {
            var route = new Route(command);
            await routeRepository.AddAsync(route, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Route, ECreateRouteError>.Success(route);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data creating route {RouteName} for agency {AgencyId}", command.Name, command.AgencyId);
            return new Result<Route, ECreateRouteError>.Failure(ECreateRouteError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            logger.LogWarning(ex, "Duplicate route name {RouteName} for agency {AgencyId}", command.Name, command.AgencyId);
            return new Result<Route, ECreateRouteError>.Failure(ECreateRouteError.DuplicateName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating route {RouteName} for agency {AgencyId}", command.Name, command.AgencyId);
            return new Result<Route, ECreateRouteError>.Failure(ECreateRouteError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Route, EEnableRouteError>> Handle(EnableRouteCommand command, CancellationToken cancellationToken)
    {
        var route = await routeRepository.FindByIdAndAgencyIdAsync(command.RouteId, command.AgencyId, cancellationToken);
        if (route is null) return new Result<Route, EEnableRouteError>.Failure(EEnableRouteError.RouteNotFound);

        try
        {
            route.Enable(await checkpointRepository.CountByRouteIdAsync(route.Id, cancellationToken));
            routeRepository.Update(route);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Route, EEnableRouteError>.Success(route);
        }
        catch (InvalidOperationException)
        {
            return new Result<Route, EEnableRouteError>.Failure(EEnableRouteError.RouteWithoutCheckpoints);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error enabling route {RouteId}", command.RouteId);
            return new Result<Route, EEnableRouteError>.Failure(EEnableRouteError.UnexpectedError);
        }
    }
}
