using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application command service interface for tourist manifest write operations.</summary>
public interface IManifestEntryCommandService
{
    /// <summary>Registers a participant in the manifest and creates its empty preparation.</summary>
    Task<Result<ManifestEntry, ECreateManifestEntryError>> Handle(CreateManifestEntryCommand command, CancellationToken cancellationToken);
}
