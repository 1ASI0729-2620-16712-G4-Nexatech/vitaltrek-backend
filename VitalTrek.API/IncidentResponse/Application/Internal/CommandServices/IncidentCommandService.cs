using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IIncidentCommandService"/>.
/// </summary>
/// <param name="incidentRepository">The incident repository.</param>
/// <param name="externalTrackingSessionService">The outbound service that validates the tracking session.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class IncidentCommandService(
    IIncidentRepository incidentRepository,
    ExternalTrackingSessionService externalTrackingSessionService,
    IUnitOfWork unitOfWork,
    ILogger<IncidentCommandService> logger) : IIncidentCommandService
{
    /// <inheritdoc />
    public async Task<Result<Incident, EDeclareIncidentError>> Handle(DeclareIncidentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await externalTrackingSessionService.ExistsSession(command.AgencyId, command.SessionId, cancellationToken))
                return Fail(EDeclareIncidentError.SessionNotFound);
            if (!await externalTrackingSessionService.IsSessionActive(command.SessionId, cancellationToken))
                return Fail(EDeclareIncidentError.SessionNotActive);

            var incident = new Incident(command, DateTimeOffset.UtcNow);
            await incidentRepository.AddAsync(incident, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Incident, EDeclareIncidentError>.Success(incident);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data declaring incident for session {SessionId}", command.SessionId);
            return Fail(EDeclareIncidentError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error declaring incident for session {SessionId}", command.SessionId);
            return Fail(EDeclareIncidentError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Incident, EUpdateIncidentStatusError>> Handle(UpdateIncidentStatusCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Enum.IsDefined(command.Status))
                return new Result<Incident, EUpdateIncidentStatusError>.Failure(EUpdateIncidentStatusError.InvalidData);

            var incident = await incidentRepository.FindByIdAndAgencyIdAsync(command.IncidentId, command.AgencyId,
                cancellationToken);
            if (incident is null)
                return new Result<Incident, EUpdateIncidentStatusError>.Failure(EUpdateIncidentStatusError.IncidentNotFound);

            switch (command.Status)
            {
                case IncidentStatus.InResponse:
                    incident.MarkInResponse();
                    break;
                case IncidentStatus.Resolved:
                    incident.Resolve(DateTimeOffset.UtcNow);
                    break;
                case IncidentStatus.Closed:
                    incident.Close();
                    break;
                default:
                    return new Result<Incident, EUpdateIncidentStatusError>.Failure(
                        EUpdateIncidentStatusError.InvalidTransition);
            }

            incidentRepository.Update(incident);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Incident, EUpdateIncidentStatusError>.Success(incident);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid status transition for incident {IncidentId}", command.IncidentId);
            return new Result<Incident, EUpdateIncidentStatusError>.Failure(EUpdateIncidentStatusError.InvalidTransition);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data updating incident {IncidentId}", command.IncidentId);
            return new Result<Incident, EUpdateIncidentStatusError>.Failure(EUpdateIncidentStatusError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating incident {IncidentId}", command.IncidentId);
            return new Result<Incident, EUpdateIncidentStatusError>.Failure(EUpdateIncidentStatusError.UnexpectedError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Incident, EResolveIncidentError>> Handle(ResolveIncidentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var incident = await incidentRepository.FindByIdAndAgencyIdAsync(command.IncidentId, command.AgencyId,
                cancellationToken);
            if (incident is null)
                return new Result<Incident, EResolveIncidentError>.Failure(EResolveIncidentError.IncidentNotFound);

            incident.Resolve(DateTimeOffset.UtcNow);
            incidentRepository.Update(incident);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Incident, EResolveIncidentError>.Success(incident);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Incident {IncidentId} is no longer open", command.IncidentId);
            return new Result<Incident, EResolveIncidentError>.Failure(EResolveIncidentError.InvalidTransition);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error resolving incident {IncidentId}", command.IncidentId);
            return new Result<Incident, EResolveIncidentError>.Failure(EResolveIncidentError.UnexpectedError);
        }
    }

    private static Result<Incident, EDeclareIncidentError> Fail(EDeclareIncidentError error) =>
        new Result<Incident, EDeclareIncidentError>.Failure(error);
}
