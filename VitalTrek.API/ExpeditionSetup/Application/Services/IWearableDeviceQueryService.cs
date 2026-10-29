using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application query service interface for wearable device read operations.</summary>
public interface IWearableDeviceQueryService
{
    /// <summary>Retrieves the wearable device items visible to the requester.</summary>
    Task<IEnumerable<WearableDevice>> Handle(GetAllWearableDevicesQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a wearable device item visible to the requester.</summary>
    Task<WearableDevice?> Handle(GetWearableDeviceByIdQuery query, CancellationToken cancellationToken);
}
