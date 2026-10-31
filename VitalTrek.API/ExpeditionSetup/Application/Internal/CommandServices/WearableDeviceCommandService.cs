using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Model;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Extensions;

namespace VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;

/// <summary>Application command service that implements <see cref="IWearableDeviceCommandService"/>.</summary>
/// <param name="wearableDeviceRepository">The wearable device repository.</param>
/// <param name="unitOfWork">The unit of work used to commit changes.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class WearableDeviceCommandService(
    IWearableDeviceRepository wearableDeviceRepository,
    IUnitOfWork unitOfWork,
    ILogger<WearableDeviceCommandService> logger) : IWearableDeviceCommandService
{
    /// <inheritdoc />
    public async Task<Result<WearableDevice, ECreateWearableDeviceError>> Handle(CreateWearableDeviceCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Code) &&
            await wearableDeviceRepository.ExistsByCodeAsync(command.Code, cancellationToken))
            return new Result<WearableDevice, ECreateWearableDeviceError>.Failure(ECreateWearableDeviceError.DuplicateCode);

        return await CommandExecutor.RunAsync(async () =>
            {
                var wearableDevice = new WearableDevice(command);
                await wearableDeviceRepository.AddAsync(wearableDevice, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                return wearableDevice;
            },
            ex => ex switch
            {
                ArgumentException => ECreateWearableDeviceError.InvalidData,
                DbUpdateException dbUpdateException when dbUpdateException.IsDuplicateKeyViolation() =>
                    ECreateWearableDeviceError.DuplicateCode,
                _ => null
            },
            ECreateWearableDeviceError.UnexpectedError, logger, "register wearable device", cancellationToken);
    }
}
