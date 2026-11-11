using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Application.Services;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.IncidentResponse.Application.Internal.CommandServices;

/// <summary>
///     Application command service that implements <see cref="IIncidentNotificationCommandService"/>.
/// </summary>
/// <param name="notificationRepository">The incident notification repository.</param>
/// <param name="incidentRepository">The incident repository used to validate the incident.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class IncidentNotificationCommandService(
    IIncidentNotificationRepository notificationRepository,
    IIncidentRepository incidentRepository,
    IUnitOfWork unitOfWork,
    ILogger<IncidentNotificationCommandService> logger) : IIncidentNotificationCommandService
{
    /// <inheritdoc />
    public async Task<Result<IncidentNotification, ESendNotificationError>> Handle(SendNotificationCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var incident = await incidentRepository.FindByIdAndAgencyIdAsync(command.IncidentId, command.AgencyId,
                cancellationToken);
            if (incident is null) return Fail(ESendNotificationError.IncidentNotFound);
            if (incident.Status == IncidentStatus.Closed) return Fail(ESendNotificationError.IncidentClosed);

            var notification = new IncidentNotification(command, DateTimeOffset.UtcNow);
            await notificationRepository.AddAsync(notification, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<IncidentNotification, ESendNotificationError>.Success(notification);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid data sending notification for incident {IncidentId}", command.IncidentId);
            return Fail(ESendNotificationError.InvalidData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending notification for incident {IncidentId}", command.IncidentId);
            return Fail(ESendNotificationError.UnexpectedError);
        }
    }

    private static Result<IncidentNotification, ESendNotificationError> Fail(ESendNotificationError error) =>
        new Result<IncidentNotification, ESendNotificationError>.Failure(error);
}
