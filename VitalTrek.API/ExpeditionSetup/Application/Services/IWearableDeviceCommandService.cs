using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application command service interface for wearable inventory write operations.</summary>
public interface IWearableDeviceCommandService
{
    /// <summary>Registers a wearable device.</summary>
    Task<Result<WearableDevice, ECreateWearableDeviceError>> Handle(CreateWearableDeviceCommand command, CancellationToken cancellationToken);
}
