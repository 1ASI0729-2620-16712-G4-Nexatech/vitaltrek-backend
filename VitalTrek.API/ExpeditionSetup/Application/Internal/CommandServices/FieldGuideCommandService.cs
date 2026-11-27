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
///     Application command service that implements <see cref="IFieldGuideCommandService"/>. Each profile receives an
///     operational code unique in the agency, so two guides with the same name stay distinguishable.
/// </summary>
/// <param name="fieldGuideRepository">The field guide repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class FieldGuideCommandService(
    IFieldGuideRepository fieldGuideRepository,
    IUnitOfWork unitOfWork,
    ILogger<FieldGuideCommandService> logger) : IFieldGuideCommandService
{
    private const int MaximumGeneratedCodeAttempts = 50;

    /// <inheritdoc />
    public async Task<Result<FieldGuide, ECreateFieldGuideError>> Handle(CreateFieldGuideCommand command,
        CancellationToken cancellationToken)
    {
        string code;
        try
        {
            code = string.IsNullOrWhiteSpace(command.Code)
                ? await NextCodeAsync(command.AgencyId, cancellationToken)
                : FieldGuide.NormalizeCode(command.Code);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid field guide code");
            return new Result<FieldGuide, ECreateFieldGuideError>.Failure(ECreateFieldGuideError.InvalidData);
        }

        if (await fieldGuideRepository.ExistsByAgencyIdAndCodeAsync(command.AgencyId, code, cancellationToken))
            return new Result<FieldGuide, ECreateFieldGuideError>.Failure(ECreateFieldGuideError.DuplicateCode);

        return await CommandExecutor.RunAsync(async () =>
            {
                var fieldGuide = new FieldGuide(command with { Code = code });
                await fieldGuideRepository.AddAsync(fieldGuide, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                return fieldGuide;
            },
            ex => ex switch
            {
                ArgumentException => ECreateFieldGuideError.InvalidData,
                DbUpdateException update when update.IsDuplicateKeyViolation() => ECreateFieldGuideError.DuplicateCode,
                _ => (ECreateFieldGuideError?)null
            },
            ECreateFieldGuideError.UnexpectedError, logger, "create field guide", cancellationToken);
    }

    private async Task<string> NextCodeAsync(int agencyId, CancellationToken cancellationToken)
    {
        var next = await fieldGuideRepository.CountByAgencyIdAsync(agencyId, cancellationToken) + 1;
        for (var attempt = 0; attempt < MaximumGeneratedCodeAttempts; attempt++, next++)
        {
            var candidate = $"GU-{next:D3}";
            if (!await fieldGuideRepository.ExistsByAgencyIdAndCodeAsync(agencyId, candidate, cancellationToken))
                return candidate;
        }

        return $"GU-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }
}
