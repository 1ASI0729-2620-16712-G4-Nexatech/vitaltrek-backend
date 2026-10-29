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
///     Application command service that implements <see cref="IRouteNoteCommandService"/>.
/// </summary>
/// <param name="routeNoteRepository">The route note repository.</param>
/// <param name="routeRepository">The route repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class RouteNoteCommandService(
    IRouteNoteRepository routeNoteRepository,
    IRouteRepository routeRepository,
    IUnitOfWork unitOfWork,
    ILogger<RouteNoteCommandService> logger) : IRouteNoteCommandService
{
    /// <inheritdoc />
    public async Task<Result<RouteNote, ERouteNoteError>> Handle(CreateRouteNoteCommand command,
        CancellationToken cancellationToken)
    {
        if (await routeRepository.FindByIdAndAgencyIdAsync(command.RouteId, command.AgencyId, cancellationToken) is null)
            return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.RouteNotFound);
        if (await routeNoteRepository.ExistsByRouteIdAndDisplayOrderAsync(command.RouteId, command.DisplayOrder,
                cancellationToken: cancellationToken))
            return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.DuplicateDisplayOrder);

        return await SaveAsync(() => new RouteNote(command), isNew: true, command.RouteId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<RouteNote, ERouteNoteError>> Handle(UpdateRouteNoteCommand command,
        CancellationToken cancellationToken)
    {
        var routeNote = await routeNoteRepository.FindByIdAndAgencyIdAsync(command.RouteNoteId, command.AgencyId,
            cancellationToken);
        if (routeNote is null) return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.RouteNoteNotFound);
        if (await routeNoteRepository.ExistsByRouteIdAndDisplayOrderAsync(routeNote.RouteId, command.DisplayOrder,
                routeNote.Id, cancellationToken))
            return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.DuplicateDisplayOrder);

        return await SaveAsync(() =>
        {
            routeNote.Update(command);
            return routeNote;
        }, isNew: false, routeNote.RouteId, cancellationToken);
    }

    private async Task<Result<RouteNote, ERouteNoteError>> SaveAsync(Func<RouteNote> apply, bool isNew, int routeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var routeNote = apply();
            if (isNew) await routeNoteRepository.AddAsync(routeNote, cancellationToken);
            else routeNoteRepository.Update(routeNote);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<RouteNote, ERouteNoteError>.Success(routeNote);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data saving route note for route {RouteId}", routeId);
            return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.DuplicateDisplayOrder);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error saving route note for route {RouteId}", routeId);
            return new Result<RouteNote, ERouteNoteError>.Failure(ERouteNoteError.UnexpectedError);
        }
    }
}
