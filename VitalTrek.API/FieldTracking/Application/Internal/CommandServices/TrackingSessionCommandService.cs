using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VitalTrek.API.FieldTracking.Application.Errors;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.FieldTracking.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="ITrackingSessionCommandService"/>.
/// </summary>
public class TrackingSessionCommandService(
    ITrackingSessionRepository trackingSessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<TrackingSessionCommandService> logger) : ITrackingSessionCommandService
{
    /// <inheritdoc />
    public async Task<Result<TrackingSession, EStartTrackingSessionError>> Handle(
        StartTrackingSessionCommand command, CancellationToken cancellationToken)
    {
        if (await trackingSessionRepository.ExistsActiveByGroupIdAsync(command.ExpeditionGroupId, cancellationToken))
            return new Result<TrackingSession, EStartTrackingSessionError>.Failure(
                EStartTrackingSessionError.SessionAlreadyActive);

        try
        {
            var session = new TrackingSession(command);
            await trackingSessionRepository.AddAsync(session, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<TrackingSession, EStartTrackingSessionError>.Success(session);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data starting tracking session.");
            return new Result<TrackingSession, EStartTrackingSessionError>.Failure(EStartTrackingSessionError.InvalidData);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error starting tracking session.");
            return new Result<TrackingSession, EStartTrackingSessionError>.Failure(EStartTrackingSessionError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TrackingSession, EEndTrackingSessionError>> Handle(
        EndTrackingSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await trackingSessionRepository.FindByIdAndAgencyIdAsync(
            command.SessionId, command.AgencyId, cancellationToken);

        if (session is null)
            return new Result<TrackingSession, EEndTrackingSessionError>.Failure(EEndTrackingSessionError.SessionNotFound);

        try
        {
            session.End();
            trackingSessionRepository.Update(session);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<TrackingSession, EEndTrackingSessionError>.Success(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Attempted to end a non-active session.");
            return new Result<TrackingSession, EEndTrackingSessionError>.Failure(EEndTrackingSessionError.SessionNotActive);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error ending tracking session.");
            return new Result<TrackingSession, EEndTrackingSessionError>.Failure(EEndTrackingSessionError.UnexpectedError);
        }
    }
}
