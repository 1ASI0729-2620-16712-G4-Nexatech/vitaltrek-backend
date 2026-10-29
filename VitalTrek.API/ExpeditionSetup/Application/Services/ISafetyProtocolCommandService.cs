using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application command service interface for safety protocol write operations.
/// </summary>
public interface ISafetyProtocolCommandService
{
    /// <summary>Configures the safety protocol of a route.</summary>
    Task<Result<SafetyProtocol, ESafetyProtocolError>> Handle(CreateSafetyProtocolCommand command, CancellationToken cancellationToken);

    /// <summary>Updates a safety protocol.</summary>
    Task<Result<SafetyProtocol, ESafetyProtocolError>> Handle(UpdateSafetyProtocolCommand command, CancellationToken cancellationToken);
}
