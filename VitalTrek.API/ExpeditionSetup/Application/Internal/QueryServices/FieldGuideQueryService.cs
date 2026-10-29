using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>Application query service that implements <see cref="IFieldGuideQueryService"/>.</summary>
/// <param name="repository">The repository.</param>
public class FieldGuideQueryService(IFieldGuideRepository repository) : IFieldGuideQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<FieldGuide>> Handle(GetAllFieldGuidesQuery query, CancellationToken cancellationToken) =>
        await repository.FindByAgencyIdAsync(query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<FieldGuide?> Handle(GetFieldGuideByIdQuery query, CancellationToken cancellationToken) =>
        await repository.FindByIdAndAgencyIdAsync(query.FieldGuideId, query.AgencyId, cancellationToken);
}
