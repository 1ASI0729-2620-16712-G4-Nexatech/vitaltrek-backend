using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>Application query service that implements <see cref="IManifestEntryQueryService"/>.</summary>
/// <param name="repository">The repository.</param>
public class ManifestEntryQueryService(IManifestEntryRepository repository) : IManifestEntryQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ManifestEntry>> Handle(GetAllManifestEntriesQuery query, CancellationToken cancellationToken) =>
        await repository.FindByScopeAsync(query.Scope, query.ExpeditionGroupId, cancellationToken);

    /// <inheritdoc />
    public async Task<ManifestEntry?> Handle(GetManifestEntryByIdQuery query, CancellationToken cancellationToken) =>
        await repository.FindByIdAndScopeAsync(query.ManifestEntryId, query.Scope, cancellationToken);
}
