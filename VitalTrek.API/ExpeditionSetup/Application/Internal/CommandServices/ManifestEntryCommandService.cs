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

/// <summary>Application command service that implements <see cref="IManifestEntryCommandService"/>.</summary>
/// <param name="manifestEntryRepository">The manifest entry repository.</param>
/// <param name="expeditionGroupRepository">The expedition group repository.</param>
/// <param name="participantPreparationRepository">The participant preparation repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class ManifestEntryCommandService(
    IManifestEntryRepository manifestEntryRepository,
    IExpeditionGroupRepository expeditionGroupRepository,
    IParticipantPreparationRepository participantPreparationRepository,
    IUnitOfWork unitOfWork,
    ILogger<ManifestEntryCommandService> logger) : IManifestEntryCommandService
{
    /// <inheritdoc />
    public async Task<Result<ManifestEntry, ECreateManifestEntryError>> Handle(CreateManifestEntryCommand command,
        CancellationToken cancellationToken)
    {
        var scope = new AccessScope(command.AgencyId, command.RequesterFieldGuideId);
        var expeditionGroup = await expeditionGroupRepository.FindByIdAndScopeAsync(command.ExpeditionGroupId, scope,
            cancellationToken);
        if (expeditionGroup is null) return Failure(ECreateManifestEntryError.ExpeditionGroupNotFound);
        if (!expeditionGroup.IsScheduled) return Failure(ECreateManifestEntryError.ExpeditionGroupNotScheduled);
        if (string.IsNullOrWhiteSpace(command.IdentityDocument)) return Failure(ECreateManifestEntryError.InvalidData);
        if (await manifestEntryRepository.ExistsByExpeditionGroupIdAndIdentityDocumentAsync(expeditionGroup.Id,
                command.IdentityDocument, cancellationToken))
            return Failure(ECreateManifestEntryError.DuplicateIdentityDocument);
        if (await manifestEntryRepository.ExistsOnDepartureDateAsync(command.AgencyId, command.IdentityDocument,
                expeditionGroup.DepartureDate, expeditionGroup.Id, cancellationToken))
            return Failure(ECreateManifestEntryError.IdentityDocumentDateConflict);
        if (await manifestEntryRepository.CountByExpeditionGroupIdAsync(expeditionGroup.Id, cancellationToken)
            >= expeditionGroup.MaximumCapacity)
            return Failure(ECreateManifestEntryError.CapacityExceeded);

        return await CommandExecutor.RunAsync(async () =>
            {
                var manifestEntry = new ManifestEntry(command);
                await manifestEntryRepository.AddAsync(manifestEntry, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                // Every registered participant starts with an empty preparation.
                await participantPreparationRepository.AddAsync(
                    new ParticipantPreparation(manifestEntry.AgencyId, manifestEntry.ExpeditionGroupId, manifestEntry.Id),
                    cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                return manifestEntry;
            },
            ex => ex switch
            {
                ArgumentException => ECreateManifestEntryError.InvalidData,
                DbUpdateException dbUpdateException when dbUpdateException.IsDuplicateKeyViolation() =>
                    ECreateManifestEntryError.DuplicateIdentityDocument,
                _ => null
            },
            ECreateManifestEntryError.UnexpectedError, logger, "register manifest entry", cancellationToken);
    }

    private static Result<ManifestEntry, ECreateManifestEntryError> Failure(ECreateManifestEntryError error) =>
        new Result<ManifestEntry, ECreateManifestEntryError>.Failure(error);
}
