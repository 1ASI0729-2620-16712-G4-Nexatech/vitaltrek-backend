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

/// <summary>Application command service that implements <see cref="IFieldGuideCommandService"/>.</summary>
/// <param name="fieldGuideRepository">The field guide repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class FieldGuideCommandService(
    IFieldGuideRepository fieldGuideRepository,
    IUnitOfWork unitOfWork,
    ILogger<FieldGuideCommandService> logger) : IFieldGuideCommandService
{
    /// <inheritdoc />
    public Task<Result<FieldGuide, ECreateFieldGuideError>> Handle(CreateFieldGuideCommand command,
        CancellationToken cancellationToken) =>
        CommandExecutor.RunAsync(async () =>
            {
                var fieldGuide = new FieldGuide(command);
                await fieldGuideRepository.AddAsync(fieldGuide, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                return fieldGuide;
            },
            ex => ex is ArgumentException ? ECreateFieldGuideError.InvalidData : null,
            ECreateFieldGuideError.UnexpectedError, logger, "create field guide", cancellationToken);
}
