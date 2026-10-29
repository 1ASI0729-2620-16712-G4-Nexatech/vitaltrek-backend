using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Model;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;

/// <summary>Application command service that implements <see cref="IExpeditionGroupCommandService"/>.</summary>
/// <param name="expeditionGroupRepository">The expedition group repository.</param>
/// <param name="routeRepository">The route repository.</param>
/// <param name="fieldGuideRepository">The field guide repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class ExpeditionGroupCommandService(
    IExpeditionGroupRepository expeditionGroupRepository,
    IRouteRepository routeRepository,
    IFieldGuideRepository fieldGuideRepository,
    IUnitOfWork unitOfWork,
    ILogger<ExpeditionGroupCommandService> logger) : IExpeditionGroupCommandService
{
    /// <inheritdoc />
    public async Task<Result<ExpeditionGroup, ECreateExpeditionGroupError>> Handle(CreateExpeditionGroupCommand command,
        CancellationToken cancellationToken)
    {
        var route = await routeRepository.FindByIdAndAgencyIdAsync(command.RouteId, command.AgencyId, cancellationToken);
        if (route is null)
            return new Result<ExpeditionGroup, ECreateExpeditionGroupError>.Failure(ECreateExpeditionGroupError.RouteNotFound);
        if (!route.IsEnabled)
            return new Result<ExpeditionGroup, ECreateExpeditionGroupError>.Failure(ECreateExpeditionGroupError.RouteNotEnabled);
        var today = AgencyCalendar.Today();
        if (command.DepartureDate < today)
            return new Result<ExpeditionGroup, ECreateExpeditionGroupError>.Failure(ECreateExpeditionGroupError.DepartureDateInPast);

        return await CommandExecutor.RunAsync(async () =>
            {
                var expeditionGroup = new ExpeditionGroup(command, today);
                await expeditionGroupRepository.AddAsync(expeditionGroup, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                return expeditionGroup;
            },
            ex => ex is ArgumentException ? ECreateExpeditionGroupError.InvalidData : null,
            ECreateExpeditionGroupError.UnexpectedError, logger, "create expedition group", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ExpeditionGroup, EAssignFieldGuideError>> Handle(AssignFieldGuideCommand command,
        CancellationToken cancellationToken)
    {
        var expeditionGroup = await expeditionGroupRepository.FindByIdAndScopeAsync(command.ExpeditionGroupId,
            new AccessScope(command.AgencyId), cancellationToken);
        if (expeditionGroup is null)
            return new Result<ExpeditionGroup, EAssignFieldGuideError>.Failure(EAssignFieldGuideError.ExpeditionGroupNotFound);
        if (!expeditionGroup.IsScheduled)
            return new Result<ExpeditionGroup, EAssignFieldGuideError>.Failure(EAssignFieldGuideError.ExpeditionGroupNotScheduled);
        if (await fieldGuideRepository.FindByIdAndAgencyIdAsync(command.FieldGuideId, command.AgencyId, cancellationToken) is null)
            return new Result<ExpeditionGroup, EAssignFieldGuideError>.Failure(EAssignFieldGuideError.FieldGuideNotFound);
        if (!command.ConfirmScheduleConflict && await expeditionGroupRepository.HasFieldGuideScheduleConflictAsync(
                command.FieldGuideId, expeditionGroup.DepartureDate, expeditionGroup.Id, cancellationToken))
            return new Result<ExpeditionGroup, EAssignFieldGuideError>.Failure(EAssignFieldGuideError.ScheduleConflict);

        return await CommandExecutor.RunAsync(async () =>
            {
                expeditionGroup.AssignFieldGuide(command.FieldGuideId);
                expeditionGroupRepository.Update(expeditionGroup);
                await unitOfWork.CompleteAsync(cancellationToken);
                return expeditionGroup;
            },
            ex => ex is InvalidOperationException ? EAssignFieldGuideError.ExpeditionGroupNotScheduled : null,
            EAssignFieldGuideError.UnexpectedError, logger, "assign field guide", cancellationToken);
    }
}
