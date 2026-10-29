using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application command service interface for expected time window write operations.
/// </summary>
public interface IExpectedTimeWindowCommandService
{
    /// <summary>Sets the expected time window of a route segment.</summary>
    Task<Result<ExpectedTimeWindow, ECreateExpectedTimeWindowError>> Handle(CreateExpectedTimeWindowCommand command,
        CancellationToken cancellationToken);
}
