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
///     Application command service that implements <see cref="ISafetyProtocolCommandService"/>.
/// </summary>
/// <param name="safetyProtocolRepository">The safety protocol repository.</param>
/// <param name="routeRepository">The route repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class SafetyProtocolCommandService(
    ISafetyProtocolRepository safetyProtocolRepository,
    IRouteRepository routeRepository,
    IUnitOfWork unitOfWork,
    ILogger<SafetyProtocolCommandService> logger) : ISafetyProtocolCommandService
{
    /// <inheritdoc />
    public async Task<Result<SafetyProtocol, ESafetyProtocolError>> Handle(CreateSafetyProtocolCommand command,
        CancellationToken cancellationToken)
    {
        if (await routeRepository.FindByIdAndAgencyIdAsync(command.RouteId, command.AgencyId, cancellationToken) is null)
            return new Result<SafetyProtocol, ESafetyProtocolError>.Failure(ESafetyProtocolError.RouteNotFound);
        if (await safetyProtocolRepository.ExistsByRouteIdAsync(command.RouteId, cancellationToken))
            return new Result<SafetyProtocol, ESafetyProtocolError>.Failure(ESafetyProtocolError.DuplicateProtocol);

        return await SaveAsync(() => new SafetyProtocol(command), isNew: true, command.RouteId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<SafetyProtocol, ESafetyProtocolError>> Handle(UpdateSafetyProtocolCommand command,
        CancellationToken cancellationToken)
    {
        var safetyProtocol = await safetyProtocolRepository.FindByIdAndAgencyIdAsync(command.SafetyProtocolId,
            command.AgencyId, cancellationToken);
        if (safetyProtocol is null)
            return new Result<SafetyProtocol, ESafetyProtocolError>.Failure(ESafetyProtocolError.SafetyProtocolNotFound);

        return await SaveAsync(() =>
        {
            safetyProtocol.Update(command);
            return safetyProtocol;
        }, isNew: false, safetyProtocol.RouteId, cancellationToken);
    }

    private async Task<Result<SafetyProtocol, ESafetyProtocolError>> SaveAsync(Func<SafetyProtocol> apply, bool isNew,
        int routeId, CancellationToken cancellationToken)
    {
        try
        {
            var safetyProtocol = apply();
            if (isNew) await safetyProtocolRepository.AddAsync(safetyProtocol, cancellationToken);
            else safetyProtocolRepository.Update(safetyProtocol);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<SafetyProtocol, ESafetyProtocolError>.Success(safetyProtocol);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data saving safety protocol for route {RouteId}", routeId);
            return new Result<SafetyProtocol, ESafetyProtocolError>.Failure(ESafetyProtocolError.InvalidData);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            return new Result<SafetyProtocol, ESafetyProtocolError>.Failure(ESafetyProtocolError.DuplicateProtocol);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error saving safety protocol for route {RouteId}", routeId);
            return new Result<SafetyProtocol, ESafetyProtocolError>.Failure(ESafetyProtocolError.UnexpectedError);
        }
    }
}
