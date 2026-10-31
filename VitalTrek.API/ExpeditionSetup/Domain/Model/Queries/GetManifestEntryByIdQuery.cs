using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a manifest entry visible to the requester.
/// </summary>
public record GetManifestEntryByIdQuery(AccessScope Scope, int ManifestEntryId);
