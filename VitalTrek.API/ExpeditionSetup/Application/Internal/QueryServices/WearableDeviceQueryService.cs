using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.QueryServices;

/// <summary>Application query service that implements <see cref="IWearableDeviceQueryService"/>.</summary>
/// <param name="repository">The repository.</param>
public class WearableDeviceQueryService(IWearableDeviceRepository repository) : IWearableDeviceQueryService
{
    /// <inheritdoc />
    public async Task<IEnumerable<WearableDevice>> Handle(GetAllWearableDevicesQuery query, CancellationToken cancellationToken) =>
        await repository.FindByAgencyIdAsync(query.AgencyId, cancellationToken);

    /// <inheritdoc />
    public async Task<WearableDevice?> Handle(GetWearableDeviceByIdQuery query, CancellationToken cancellationToken) =>
        await repository.FindByIdAndAgencyIdAsync(query.WearableDeviceId, query.AgencyId, cancellationToken);
}
