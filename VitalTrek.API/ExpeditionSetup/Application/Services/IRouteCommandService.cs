using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application command service interface for route write operations.
/// </summary>
public interface IRouteCommandService
{
    /// <summary>Registers a new route in draft status.</summary>
    Task<Result<Route, ECreateRouteError>> Handle(CreateRouteCommand command, CancellationToken cancellationToken);

    /// <summary>Enables a route that has at least one checkpoint.</summary>
    Task<Result<Route, EEnableRouteError>> Handle(EnableRouteCommand command, CancellationToken cancellationToken);
}
