using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>
///     Application query service that implements <see cref="IExpectedTimeWindowQueryService"/>.
/// </summary>
/// <param name="expectedTimeWindowRepository">The repository.</param>
public class ExpectedTimeWindowQueryService(IExpectedTimeWindowRepository expectedTimeWindowRepository) : IExpectedTimeWindowQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ExpectedTimeWindow>> Handle(GetAllExpectedTimeWindowsQuery query, CancellationToken cancellationToken) =>
        await expectedTimeWindowRepository.FindByAgencyIdAsync(query.AgencyId, query.RouteId, cancellationToken);

    /// <inheritdoc />
    public async Task<ExpectedTimeWindow?> Handle(GetExpectedTimeWindowByIdQuery query, CancellationToken cancellationToken) =>
        await expectedTimeWindowRepository.FindByIdAndAgencyIdAsync(query.ExpectedTimeWindowId, query.AgencyId, cancellationToken);
}
