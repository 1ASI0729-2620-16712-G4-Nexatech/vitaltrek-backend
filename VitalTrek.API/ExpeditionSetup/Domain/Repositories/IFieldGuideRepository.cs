using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Domain.Repositories;

/// <summary>
///     Domain repository interface for managing <see cref="FieldGuide"/> aggregates.
/// </summary>
public interface IFieldGuideRepository : IBaseRepository<FieldGuide>
{
    /// <summary>Finds the field guides of an agency.</summary>
    Task<IEnumerable<FieldGuide>> FindByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds a field guide that belongs to an agency.</summary>
    /// <summary>Returns true when the operational code is already used in the agency.</summary>
    Task<bool> ExistsByAgencyIdAndCodeAsync(int agencyId, string code, CancellationToken cancellationToken = default);

    /// <summary>Counts the field guide profiles of an agency.</summary>
    Task<int> CountByAgencyIdAsync(int agencyId, CancellationToken cancellationToken = default);

    /// <summary>Finds a field guide profile of an agency.</summary>
    Task<FieldGuide?> FindByIdAndAgencyIdAsync(int fieldGuideId, int agencyId, CancellationToken cancellationToken = default);
}
