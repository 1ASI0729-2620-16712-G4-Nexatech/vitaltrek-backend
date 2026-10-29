using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application command service interface for route note write operations.
/// </summary>
public interface IRouteNoteCommandService
{
    /// <summary>Registers a route note.</summary>
    Task<Result<RouteNote, ERouteNoteError>> Handle(CreateRouteNoteCommand command, CancellationToken cancellationToken);

    /// <summary>Updates a route note.</summary>
    Task<Result<RouteNote, ERouteNoteError>> Handle(UpdateRouteNoteCommand command, CancellationToken cancellationToken);
}
