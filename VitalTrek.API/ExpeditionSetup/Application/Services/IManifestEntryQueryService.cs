using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application query service interface for manifest entry read operations.</summary>
public interface IManifestEntryQueryService
{
    /// <summary>Retrieves the manifest entry items visible to the requester.</summary>
    Task<IEnumerable<ManifestEntry>> Handle(GetAllManifestEntriesQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a manifest entry item visible to the requester.</summary>
    Task<ManifestEntry?> Handle(GetManifestEntryByIdQuery query, CancellationToken cancellationToken);
}
