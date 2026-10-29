using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the manifest entries visible to the requester, optionally filtered by group.
/// </summary>
public record GetAllManifestEntriesQuery(AccessScope Scope, int? ExpeditionGroupId);
